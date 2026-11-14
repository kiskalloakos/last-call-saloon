using System;
using System.Linq;
using UnityEngine;

namespace Saloon
{
    // Local preferences only: rendering settings never affect match rules or other players.
    public sealed class SaloonGraphics : MonoBehaviour
    {
        const string PreferenceKey="SaloonGraphics.v1";
        public static readonly string[] PresetNames={"LOW","MEDIUM","HIGH"};
        public static readonly int[] FrameLimits={60,90,120,144,165,240,0};

        [Serializable]
        public struct Options
        {
            public int preset,width,height,frameLimit;
            public bool windowed,vsync;
        }

        public Options Current { get; private set; }
        public Vector2Int[] Resolutions { get; private set; }
        public bool AwaitingConfirmation { get; private set; }
        public float ConfirmationRemaining=>Mathf.Max(0,confirmUntil-Time.realtimeSinceStartup);
        Options previous;
        float confirmUntil;

        void Awake()
        {
            var current=new Vector2Int(Mathf.Max(640,Screen.width),Mathf.Max(360,Screen.height));
            Resolutions=Screen.resolutions.Select(r=>new Vector2Int(r.width,r.height))
                .Where(r=>r.x>=640&&r.y>=360).Append(current).Distinct()
                .OrderBy(r=>r.x).ThenBy(r=>r.y).ToArray();
            var options=Defaults();
            if(PlayerPrefs.HasKey(PreferenceKey))
            {
                try{options=JsonUtility.FromJson<Options>(PlayerPrefs.GetString(PreferenceKey));}
                catch(ArgumentException){options=Defaults();}
            }
            Current=Normalize(options);
            ApplyRuntime(Current,PlayerPrefs.HasKey(PreferenceKey));
        }

        public Options Defaults()=>new Options{preset=1,width=Screen.width,height=Screen.height,
            windowed=Screen.fullScreenMode==FullScreenMode.Windowed,frameLimit=60,vsync=false};

        Options Normalize(Options options)
        {
            options.preset=Mathf.Clamp(options.preset,0,PresetNames.Length-1);
            if(!FrameLimits.Contains(options.frameLimit))options.frameLimit=60;
            if(!Resolutions.Contains(new Vector2Int(options.width,options.height)))
            {options.width=Mathf.Max(640,Screen.width);options.height=Mathf.Max(360,Screen.height);}
            return options;
        }

        public void Apply(Options options)
        {
            if(AwaitingConfirmation)return;
            previous=Current;Current=Normalize(options);
            bool displayChanged=Current.width!=previous.width||Current.height!=previous.height||Current.windowed!=previous.windowed;
            ApplyRuntime(Current,displayChanged);
            if(displayChanged&&!Application.isEditor)
            {AwaitingConfirmation=true;confirmUntil=Time.realtimeSinceStartup+15;}
            else Save();
        }

        void ApplyRuntime(Options options,bool changeDisplay)
        {
            // Start with the matching built-in preset, then explicitly set the costs this game uses.
            string qualityName=options.preset==0?"Low":options.preset==1?"Medium":"High";
            int quality=Array.IndexOf(QualitySettings.names,qualityName);
            if(quality>=0)QualitySettings.SetQualityLevel(quality,true);
            QualitySettings.pixelLightCount=options.preset==0?2:options.preset==1?4:8;
            QualitySettings.shadows=options.preset==0?ShadowQuality.Disable:options.preset==1?ShadowQuality.HardOnly:ShadowQuality.All;
            QualitySettings.shadowResolution=options.preset==2?ShadowResolution.High:ShadowResolution.Low;
            QualitySettings.shadowDistance=options.preset==0?0:options.preset==1?25:50;
            QualitySettings.shadowCascades=options.preset==2?2:1;
            QualitySettings.antiAliasing=options.preset==2?4:0;
            QualitySettings.globalTextureMipmapLimit=options.preset==0?1:0;
            QualitySettings.anisotropicFiltering=options.preset==0?AnisotropicFiltering.Disable:AnisotropicFiltering.Enable;
            QualitySettings.realtimeReflectionProbes=options.preset==2;
            QualitySettings.vSyncCount=options.vsync?1:0;
            Application.targetFrameRate=options.frameLimit==0?-1:options.frameLimit;
            if(changeDisplay&&!Application.isEditor)
                Screen.SetResolution(options.width,options.height,options.windowed?FullScreenMode.Windowed:FullScreenMode.FullScreenWindow);
        }

        public void Keep()
        {
            if(!AwaitingConfirmation)return;
            AwaitingConfirmation=false;Save();
        }

        public void Revert()
        {
            if(!AwaitingConfirmation)return;
            AwaitingConfirmation=false;Current=previous;ApplyRuntime(Current,true);
        }

        void Save(){PlayerPrefs.SetString(PreferenceKey,JsonUtility.ToJson(Current));PlayerPrefs.Save();}
        void Update(){if(AwaitingConfirmation&&Time.realtimeSinceStartup>=confirmUntil)Revert();}
        void OnDisable(){Revert();}
    }
}
