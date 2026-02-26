using System.Collections.Concurrent;
using TheWatch.P7.FamilyHealth.Family;

namespace TheWatch.P7.FamilyHealth.Services;

public interface IFamilyService
{
    Task<FamilyGroup> CreateGroupAsync(CreateFamilyGroupRequest request);
    Task<FamilyGroupResponse?> GetGroupAsync(Guid groupId);
    Task<List<FamilyGroup>> ListGroupsAsync();
    Task<FamilyMember> AddMemberAsync(Guid groupId, AddMemberRequest request);
    Task<FamilyMember?> GetMemberAsync(Guid memberId);
    Task<bool> RemoveMemberAsync(Guid memberId);
}

public class FamilyService : IFamilyService
{
    private readonly ConcurrentDictionary<Guid, FamilyGroup> _groups = new();
    private readonly ConcurrentDictionary<Guid, FamilyMember> _members = new();

    public Task<FamilyGroup> CreateGroupAsync(CreateFamilyGroupRequest request)
    {
        var group = new FamilyGroup { Name = request.Name };
        _groups[group.Id] = group;
        return Task.FromResult(group);
    }

    public Task<FamilyGroupResponse?> GetGroupAsync(Guid groupId)
    {
        if (!_groups.TryGetValue(groupId, out var group))
            return Task.FromResult<FamilyGroupResponse?>(null);

        var members = _members.Values
            .Where(m => m.FamilyGroupId == groupId)
            .ToList();
        return Task.FromResult<FamilyGroupResponse?>(new FamilyGroupResponse(group, members));
    }

    public Task<List<FamilyGroup>> ListGroupsAsync()
    {
        return Task.FromResult(_groups.Values.OrderBy(g => g.Name).ToList());
    }

    public Task<FamilyMember> AddMemberAsync(Guid groupId, AddMemberRequest request)
    {
        var member = new FamilyMember
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Role = request.Role,
            FamilyGroupId = groupId
        };

        _members[member.Id] = member;

        if (_groups.TryGetValue(groupId, out var group))
            group.MemberIds.Add(member.Id);

        return Task.FromResult(member);
    }

    public Task<FamilyMember?> GetMemberAsync(Guid memberId)
    {
        _members.TryGetValue(memberId, out var member);
        return Task.FromResult(member);
    }

    public Task<bool> RemoveMemberAsync(Guid memberId)
    {
        if (!_members.TryRemove(memberId, out var member))
            return Task.FromResult(false);

        if (_groups.TryGetValue(member.FamilyGroupId, out var group))
            group.MemberIds.Remove(memberId);

        return Task.FromResult(true);
    }
}
