using System;
using System.Collections.Generic;
using UnityEngine;

namespace Saloon
{
    // Small, readable playing-card artwork generated locally; no asset downloads.
    public sealed class SaloonCardArt : IDisposable
    {
        public const float Width=.19f,Length=.28f;
        public readonly Mesh Mesh;
        public readonly Material Back,Edge;
        readonly Material[] faces=new Material[52];
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        const int W=128,H=192;
        static readonly Color Paper=new Color(.96f,.92f,.81f),Red=new Color(.65f,.055f,.06f),Black=new Color(.07f,.065f,.06f);
        public SaloonCardArt()
        {
            Mesh=CardMesh();owned.Add(Mesh);
            var pixels=new Color[W*H];
            for(int y=0;y<H;y++)for(int x=0;x<W;x++)
            {
                bool border=x<7||x>=W-7||y<7||y>=H-7;
                bool weave=(x+y)%12<2||(x-y+H)%12<2;
                pixels[y*W+x]=border?Paper:weave?new Color(.70f,.38f,.25f):new Color(.28f,.055f,.045f);
                if(Mathf.Abs(x-W/2)+Mathf.Abs(y-H/2)<25)pixels[y*W+x]=new Color(.82f,.66f,.35f);
                if(Mathf.Abs(x-W/2)+Mathf.Abs(y-H/2)<16)pixels[y*W+x]=new Color(.28f,.055f,.045f);
            }
            Back=Material("Embossed wine-red card back",pixels);
            Edge=new Material(Shader.Find("Standard")){name="Cardstock edges",color=Paper};owned.Add(Edge);
        }
        public Material Face(int card)
        {
            if(card<0)return Back;
            if(faces[card]!=null)return faces[card];
            int rank=card%13+1,suit=card/13;Color ink=suit==1||suit==2?Red:Black;
            var pixels=new Color[W*H];for(int i=0;i<pixels.Length;i++)pixels[i]=Paper;
            for(int y=4;y<H-4;y++)for(int x=4;x<W-4;x++)
                if(x==4||x==W-5||y==4||y==H-5)pixels[y*W+x]=new Color(.75f,.69f,.56f);
            string value=rank==1?"A":rank==11?"J":rank==12?"Q":rank==13?"K":rank.ToString();
            Glyph(pixels,value,10,H-13,3,ink);
            Suit(pixels,17,H-50,8,suit,ink);
            // Opposing corner indices are readable from either end of the table.
            for(int y=H-58;y<H-7;y++)for(int x=8;x<47;x++)pixels[(H-1-y)*W+W-1-x]=pixels[y*W+x];
            if(rank==1)Suit(pixels,W/2,H/2,27,suit,ink);
            else if(rank>10)
            {
                for(int y=54;y<138;y++)for(int x=37;x<91;x++)
                    if(x==37||x==90||y==54||y==137)pixels[y*W+x]=ink;
                Glyph(pixels,value,51,124,5,ink);Suit(pixels,64,77,15,suit,ink);
            }
            else
            {
                // Two columns of pips, plus a centre pip for odd ranks.
                int pairs=rank/2;
                for(int i=0;i<pairs;i++)
                {
                    int y=pairs==1?H/2:55+i*82/(pairs-1);
                    Suit(pixels,45,y,11,suit,ink);Suit(pixels,83,y,11,suit,ink);
                }
                if(rank%2==1)Suit(pixels,64,H/2,11,suit,ink);
            }
            return faces[card]=Material("Playing card "+SaloonBlackjack.Label(card),pixels);
        }
        Material Material(string name,Color[] pixels)
        {
            var texture=new Texture2D(W,H,TextureFormat.RGBA32,true){name=name,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=4};
            texture.SetPixels(pixels);texture.Apply(true,true);owned.Add(texture);
            var material=new Material(Shader.Find("Standard")){name=name,mainTexture=texture,color=Color.white};material.SetFloat("_Glossiness",.16f);owned.Add(material);return material;
        }
        static void Suit(Color[] pixels,int cx,int cy,int radius,int suit,Color color)
        {
            for(int dy=-radius;dy<=radius;dy++)for(int dx=-radius;dx<=radius;dx++)
            {
                float x=dx/(float)radius,y=dy/(float)radius;
                bool fill;
                if(suit==1)fill=Mathf.Abs(x)+Mathf.Abs(y)<.95f; // diamond
                else if(suit==2)fill=Heart(x,y); // heart
                else if(suit==3)fill=Heart(x,-y)||Stem(x,y); // spade
                else fill=(x*x+(y-.42f)*(y-.42f)<.27f)||((x-.42f)*(x-.42f)+y*y<.25f)||((x+.42f)*(x+.42f)+y*y<.25f)||Stem(x,y);
                int px=cx+dx,py=cy+dy;if(fill&&px>=0&&px<W&&py>=0&&py<H)pixels[py*W+px]=color;
            }
        }
        static bool Heart(float x,float y)
        {x*=1.15f;y=y*1.15f+.12f;float a=x*x+y*y- .72f;return a*a*a-x*x*y*y*y<0;}
        static bool Stem(float x,float y)=>y<-.25f&&y>-.95f&&Mathf.Abs(x)<.1f+(-y-.25f)*.25f;
        static readonly Dictionary<char,string> Glyphs=new Dictionary<char,string>{
            {'A',"01110100011000111111100011000110001"},{'K',"10001100101010011000101001001010001"},
            {'Q',"01110100011000110001101011001001101"},{'J',"00111000100001000010100101001001100"},
            {'0',"01110100011001110101110011000101110"},{'1',"00100011000010000100001000010001110"},
            {'2',"01110100010000100010001000100011111"},{'3',"11110000010000101110000010000111110"},
            {'4',"00010001100101010010111110001000010"},{'5',"11111100001000011110000010000111110"},
            {'6',"01110100001000011110100011000101110"},{'7',"11111000010001000100010000100001000"},
            {'8',"01110100011000101110100011000101110"},{'9',"01110100011000101111000010000101110"}};
        static void Glyph(Color[] pixels,string text,int left,int top,int scale,Color color)
        {
            foreach(char c in text)
            {
                string bits=Glyphs[c];
                for(int row=0;row<7;row++)for(int col=0;col<5;col++)if(bits[row*5+col]=='1')
                    for(int sy=0;sy<scale;sy++)for(int sx=0;sx<scale;sx++)
                    {int x=left+col*scale+sx,y=top-row*scale-sy;if(x>=0&&x<W&&y>=0&&y<H)pixels[y*W+x]=color;}
                left+=6*scale;
            }
        }
        static Mesh CardMesh()
        {
            const int segments=6;const float radius=.012f,thickness=.002f;
            var ring=new List<Vector2>();
            for(int corner=0;corner<4;corner++)for(int i=0;i<=segments;i++)
            {
                float angle=(corner*90+i*90f/segments)*Mathf.Deg2Rad;
                float cx=corner==0||corner==3?Width/2-radius:-Width/2+radius;
                float cy=corner<2?Length/2-radius:-Length/2+radius;
                ring.Add(new Vector2(cx+Mathf.Cos(angle)*radius,cy+Mathf.Sin(angle)*radius));
            }
            int n=ring.Count;var vertices=new Vector3[n*2+2];var uv=new Vector2[vertices.Length];
            for(int i=0;i<n;i++)
            {var p=ring[i];vertices[i]=new Vector3(p.x,thickness/2,p.y);vertices[n+i]=new Vector3(p.x,-thickness/2,p.y);uv[i]=uv[n+i]=new Vector2(p.x/Width+.5f,p.y/Length+.5f);}
            vertices[2*n]=Vector3.up*thickness/2;vertices[2*n+1]=Vector3.down*thickness/2;uv[2*n]=uv[2*n+1]=new Vector2(.5f,.5f);
            var front=new List<int>();var back=new List<int>();var edges=new List<int>();
            for(int i=0;i<n;i++)
            {int j=(i+1)%n;front.AddRange(new[]{2*n,j,i});back.AddRange(new[]{2*n+1,n+i,n+j});edges.AddRange(new[]{i,j,n+j,i,n+j,n+i});}
            var mesh=new Mesh{name="Rounded playing card",vertices=vertices,uv=uv,subMeshCount=3};
            mesh.SetTriangles(front,0);mesh.SetTriangles(back,1);mesh.SetTriangles(edges,2);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public void Dispose(){foreach(var obj in owned)if(obj!=null)UnityEngine.Object.Destroy(obj);}
    }
}
