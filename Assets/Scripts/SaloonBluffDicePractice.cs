using UnityEngine;

namespace Saloon
{
    // Attached only to the dedicated test scene, which is excluded from release builds.
    public sealed class SaloonBluffDicePractice : MonoBehaviour
    {
        SaloonGame game;
        void Start()
        {
            game=FindAnyObjectByType<SaloonGame>();
            if(game==null)game=new GameObject("Saloon Prototype").AddComponent<SaloonGame>();
            game.StartDicePractice();
        }
        void Update()
        {
            if(game==null||!game.DicePractice)return;
            if(!game.MenuOpen&&Input.GetKeyDown(KeyCode.N))game.NewDicePracticeHand();
        }
    }
}
