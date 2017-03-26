namespace Shinoa.Attributes
{
    using System.Linq;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Services;
    using SQLite;

    /// <summary>
    /// PreconditionAttribute that makes users required to be not blacklisted for certain commands.
    /// </summary>
    public class RequireNotBlacklistedAttribute : PreconditionAttribute
    {
        /// <summary>
        /// Checks the permission to use the command.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="command">The command used.</param>
        /// <param name="map">The dependency map.</param>
        /// <returns></returns>
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            if (!map.TryGet(out SQLiteConnection db) || context.Guild == null) return Task.FromResult(PreconditionResult.FromSuccess());
            db.CreateTable<BlacklistService.BlacklistUserBinding>();

            return db.Table<BlacklistService.BlacklistUserBinding>().Any(b => b.GuildId == context.Guild.Id.ToString() && b.UserId == context.User.Id.ToString()) ? Task.FromResult(PreconditionResult.FromError("You are not allowed to use commands on this server.")) : Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}