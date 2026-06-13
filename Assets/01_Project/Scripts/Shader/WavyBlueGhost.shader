Shader "Custom/RGBGlitchGhost"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        
        [Header(Glitch Settings)]
        _GlitchStrength ("Glitch Distance", Float) = 0.05    // 殘影分離的距離 (數值越小越精緻)
        _GlitchSpeed ("Glitch Speed", Float) = 8.0           // 殘影蠕動的速度
        _GhostAlpha ("Overall Transparency", Range(0, 1)) = 0.7 // 殘影整體的透明度
        _Tint ("Global Tint", Color) = (0.8, 0.9, 1.0, 1.0)  // 整體染色 (預設微微偏冷藍色)
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _GlitchStrength;
            float _GlitchSpeed;
            float _GhostAlpha;
            fixed4 _Tint;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color; 
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float t = _Time.y * _GlitchSpeed;

                // 【核心運算】：讓 R, G, B 三個通道朝著不同的三角函數軌跡移動
                // 使用不同的質數乘數，確保它們的移動軌跡看起來是隨機且混亂的
                float2 offsetR = float2(sin(t * 1.1), cos(t * 0.8)) * _GlitchStrength;
                float2 offsetG = float2(sin(t * 1.3 + 1.0), cos(t * 1.5 + 1.0)) * _GlitchStrength;
                float2 offsetB = float2(sin(t * 0.9 + 2.0), cos(t * 1.2 + 2.0)) * _GlitchStrength;

                // 分別在不同的偏移位置上取樣貼圖
                fixed4 colR = tex2D(_MainTex, uv + offsetR);
                fixed4 colG = tex2D(_MainTex, uv + offsetG); // G 通道也可以設為不偏移 (只用 uv)，會更有靈魂出竅感
                fixed4 colB = tex2D(_MainTex, uv + offsetB);

                // 【重組顏色】：把偏移後的紅、綠、藍重新組合在一起
                fixed4 finalColor = fixed4(colR.r, colG.g, colB.b, 1.0);

                // 【處理透明度】：只要任何一個通道有圖案(Alpha > 0)，該像素就要顯示
                // 這樣才能完美保留殘影邊緣的剪影
                finalColor.a = max(colR.a, max(colG.a, colB.a));

                // 疊加整體的透明度與染色
                finalColor.a *= _GhostAlpha;
                finalColor *= _Tint;
                
                // 完美支援 SpriteRenderer 上的 Color 設定 (淡入淡出)
                finalColor *= IN.color;

                return finalColor;
            }
            ENDCG
        }
    }
}