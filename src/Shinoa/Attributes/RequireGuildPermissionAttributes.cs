using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Shinoa.Attributes
{
    public class RequireGuildUserPermissionAttribute : RequireUserPermissionAttribute
    {
        public RequireGuildUserPermissionAttribute(GuildPermission permission) : base (permission) { }
        public RequireGuildUserPermissionAttribute(ChannelPermission permission) : base (permission) { }

        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            if (context.Guild == null) return PreconditionResult.FromSuccess();
            return await base.CheckPermissions(context, command, map);
        }
    }

    public class RequireGuildBotPermissionAttribute : RequireBotPermissionAttribute
    {
        public RequireGuildBotPermissionAttribute(GuildPermission permission) : base(permission) { }
        public RequireGuildBotPermissionAttribute(ChannelPermission permission) : base(permission) { }

        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            if (context.Guild == null) return PreconditionResult.FromSuccess();
            return await base.CheckPermissions(context, command, map);
        }
    }
}
