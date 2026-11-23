using UnityEngine;

namespace Saloon
{
    public sealed class SaloonToiletAudio : MonoBehaviour
    {
        SaloonGame game;
        AudioSource plunger,gurgle,flush;
        int round=-1,pumps;
        bool initialized;
        public void Initialize(SaloonGame owner)
        {game=owner;plunger=Source("Plunger");gurgle=Source("Gurgle");flush=Source("Flush");}
        AudioSource Source(string clip)
        {
            var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;
            source.clip=Resources.Load<AudioClip>("Audio/Toilet/"+clip);source.spatialBlend=1;source.minDistance=1.4f;source.maxDistance=8;
            source.dopplerLevel=0;source.rolloffMode=AudioRolloffMode.Linear;return source;
        }
        bool Inside=>game.InRoom&&!game.TutorialActive&&game.CameraPlayer?.room==SaloonBathroom.Room&&Active;
        bool Active=>game.View?.phase==Phase.Turns||game.View?.phase==Phase.Aftermath;
        bool CompletionTail=>game.InRoom&&Active&&game.View.toiletCompleted&&!game.IsSpectating&&game.Self?.alive==true&&game.CameraPlayer?.id==game.LocalId&&game.CameraPlayer.room==0;
        void Configure()
        {
            bool inside=Inside,completion=CompletionTail;
            plunger.volume=.55f;gurgle.volume=.32f;flush.volume=.65f;
            plunger.mute=gurgle.mute=!inside;flush.mute=!inside&&!completion;
            // Let only the worker hear their completion flush after the immediate return.
            flush.spatialBlend=completion?0:1;
            if(!inside){plunger.Stop();gurgle.Stop();}
            if(!inside&&!completion)flush.Stop();
        }
        public void Apply(Snapshot view)
        {
            Configure();
            if(!initialized||view.round!=round||view.toiletPumps<pumps)
            {Stop();initialized=true;round=view.round;pumps=view.toiletPumps;return;}
            if(Active&&view.toiletPumps>pumps&&view.toiletPumpAge>=0&&view.toiletPumpAge<.75f)
            {
                if(Inside){plunger.pitch=1+(view.toiletPumps%3-1)*.035f;plunger.Play();gurgle.pitch=1+(view.toiletPumps%2)*.04f;gurgle.Play();}
                if((Inside||CompletionTail)&&view.toiletPumps>=SaloonGame.ToiletPumpCount)flush.PlayDelayed(.12f);
            }
            if(!Active||!view.toiletInUse&&view.toiletPumps<SaloonGame.ToiletPumpCount)Stop();
            pumps=view.toiletPumps;
        }
        void Update(){if(game!=null)Configure();}
        void Stop(){plunger.Stop();gurgle.Stop();flush.Stop();}
        void OnDisable(){if(initialized)Stop();}
    }
}
