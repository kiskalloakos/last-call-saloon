using System.Linq;

namespace Saloon
{
    public partial class SaloonGame
    {
        // Keep achievements on host participants so reconnects retain them.
        // Safe-work milestones are credited to all Outlaws; other achievements
        // stay personal. Rematches clear them in ResetHeist.
        void ObserveStoryProgress(Participant own)
        {
            if(phase==Phase.Lobby)return;
            if(phase==Phase.Turns&&own.room==0&&own.Alive)own.storyProgress|=StoryProgress.Bar;
            // Hidden factions are no longer certified by observing a body. A
            // Renegade must deduce allegiances without private elimination flags.
        }
    }
}
