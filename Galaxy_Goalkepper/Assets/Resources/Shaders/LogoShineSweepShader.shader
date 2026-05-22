// UI shader for the logo shine-sweep effect. The Image using this shader is set up
// with the SAME sprite as the Logo it overlays — so the texcoord coming into the
// fragment shader is in the logo's own UV space, and `tex2D(_MainTex, uv).a` is the
// logo's exact sampled alpha at that pixel.
//
// The shine is drawn as a moving diagonal band of brightness whose alpha is then
// MULTIPLIED by that logo alpha. Result: the shine is at full intensity where the
// logo is fully opaque, smoothly fades through the logo's anti-aliased edge, and
// vanishes where the logo is fully transparent. No separate mask needed, no stencil
// cutoff at a hardcoded threshold, no bleed into the logo's surrounding transparent
// pixels — because there's no rendering happening there in the first place.
//
// All position math is in UV space (0..1 across the logo's sprite), so the rendering
// is identical in Scene view, Game view, and built players regardless of canvas
// mode (Screen Space Overlay / Camera / World Space).
Shader "UI/LogoShineSweep"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite (Logo)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineProgress ("Shine Progress", Range(-0.5, 1.5)) = -0.5
        _ShineThickness ("Shine Thickness (UV)", Range(0.01, 1)) = 0.15
        _ShineTilt ("Shine Tilt (deg)", Range(-90, 90)) = 25

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _ShineColor;
            float _ShineProgress;
            float _ShineThickness;
            float _ShineTilt;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Sample the LOGO at this pixel. This Image is set up to use the same
                // sprite as the Logo, so logoSample.a IS the logo's authentic alpha here.
                fixed4 logoSample = tex2D(_MainTex, IN.texcoord);

                // Project this pixel's UV onto the tilted sweep axis. uvProj runs 0..1
                // along the diagonal direction. Subtracting 0.5 first centers the
                // projection so the corners come out near 0 and 1.
                float angle = _ShineTilt * 0.01745329; // deg -> rad
                float2 dir = float2(cos(angle), sin(angle));
                float uvProj = dot(IN.texcoord - 0.5, dir) + 0.5;

                // Soft band centered on _ShineProgress, width = _ShineThickness.
                // smoothstep makes the band fade out at its left/right edges so the
                // shine doesn't have a hard rectangular boundary.
                float dist = abs(uvProj - _ShineProgress);
                float bandIntensity = 1.0 - smoothstep(0.0, _ShineThickness, dist);

                // Output: shine color tinted by the vertex color, alpha multiplied by
                // (logo alpha * band intensity * shine color alpha). The logo alpha
                // multiplication is the key — anywhere the logo is transparent, this
                // shine pixel is also transparent. No edge bleed possible.
                fixed4 col;
                col.rgb = _ShineColor.rgb * IN.color.rgb;
                col.a = logoSample.a * bandIntensity * _ShineColor.a * IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
