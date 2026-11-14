using System.Linq;

namespace Saloon
{
    public partial class SaloonGame
    {
        // Keep achievements on host participants so reconnects retain them.
        // Safe-work milestones are credited to both Outlaws; other achievements
        // stay personal. Rematches clear them in ResetHeist.
        void ObserveStoryProgress(Participant own)
        {
            if(phase==Phase.Lobby)return;
            if(phase==Phase.Turns&&own.room==0&&own.Alive)own.storyProgress|=StoryProgress.Bar;
            if(own.role!=Role.Renegade)return;
            foreach(var person in players.Values.Where(p=>!p.Alive&&SaloonPrivacy.ShareActivity(phase,own.room,p.room)))
                own.storySeenDead.Add(person.id);
            bool SeenAll(Role role)=>players.Values.Where(p=>p.role==role).All(p=>own.storySeenDead.Contains(p.id));
            if(SeenAll(Role.Outlaw))own.storyProgress|=StoryProgress.OutlawsGone;
            if(SeenAll(Role.Sheriff)&&SeenAll(Role.Deputy))own.storyProgress|=StoryProgress.LawGone;
        }
    }
}
