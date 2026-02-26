using Microsoft.EntityFrameworkCore;
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
    private readonly IWatchRepository<FamilyGroup> _groups;
    private readonly IWatchRepository<FamilyMember> _members;

    public FamilyService(IWatchRepository<FamilyGroup> groups, IWatchRepository<FamilyMember> members)
    {
        _groups = groups;
        _members = members;
    }

    public async Task<FamilyGroup> CreateGroupAsync(CreateFamilyGroupRequest request)
    {
        var group = new FamilyGroup { Name = request.Name };
        return await _groups.AddAsync(group);
    }

    public async Task<FamilyGroupResponse?> GetGroupAsync(Guid groupId)
    {
        var group = await _groups.GetByIdAsync(groupId);
        if (group is null) return null;

        var members = await _members.Query()
            .Where(m => m.FamilyGroupId == groupId)
            .ToListAsync();

        return new FamilyGroupResponse(group, members);
    }

    public async Task<List<FamilyGroup>> ListGroupsAsync()
    {
        return await _groups.Query().OrderBy(g => g.Name).ToListAsync();
    }

    public async Task<FamilyMember> AddMemberAsync(Guid groupId, AddMemberRequest request)
    {
        var member = new FamilyMember
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Role = request.Role,
            FamilyGroupId = groupId
        };

        await _members.AddAsync(member);

        var group = await _groups.GetByIdAsync(groupId);
        if (group is not null)
        {
            group.MemberIds.Add(member.Id);
            await _groups.UpdateAsync(group);
        }

        return member;
    }

    public async Task<FamilyMember?> GetMemberAsync(Guid memberId)
    {
        return await _members.GetByIdAsync(memberId);
    }

    public async Task<bool> RemoveMemberAsync(Guid memberId)
    {
        var member = await _members.GetByIdAsync(memberId);
        if (member is null) return false;

        var group = await _groups.GetByIdAsync(member.FamilyGroupId);
        if (group is not null)
        {
            group.MemberIds.Remove(memberId);
            await _groups.UpdateAsync(group);
        }

        await _members.DeleteAsync(memberId);
        return true;
    }
}
