using System.Collections.Generic;
using UnityEngine;

public static class Math {
    public const float PI = 3.14159265359f;

    public static float Sqrt(float x) {
        if(x < 0f) throw new System.Exception();
        if(x == 0f) return 0f;

        float res = x;
        for(int i = 0; i < 100; i++) {
            float otherSide = x / res;
            res = (res + otherSide) * 0.5f;
        }
        return res;
    }

    public static float Sin(float x) {
        x %= 2f * PI;
        float cur = x;
        float x2 = x * x;
        float res = cur;
        for(float i = 3f; i < 100f; i += 2f) {
            cur *= x2;
            cur /= i * (i - 1f);
            cur *= -1f;
            res += cur;
        }
        return res;
    }

    public static float Cos(float x) {
        x %= 2f * PI;
        float cur = 1;
        float x2 = x * x;
        float res = cur;
        for(float i = 2f; i < 99f; i += 2f) {
            cur *= x2;
            cur /= i * (i - 1f);
            cur *= -1f;
            res += cur;
        }
        return res;
    }
}

public struct V2 {
    public float x;
    public float y;

    public static V2 operator +(V2 a, V2 b) { return new V2 { x = a.x + b.x, y = a.y + b.y }; }
    public static V2 operator -(V2 a, V2 b) { return new V2 { x = a.x - b.x, y = a.y - b.y }; }
    public static V2 operator *(V2 v, float s) { return new V2 { x = v.x * s, y = v.y * s }; }
    public static V2 operator *(float s, V2 v) { return v * s; }
    public static V2 operator /(V2 v, float s) { return new V2 { x = v.x / s, y = v.y / s }; }
    public static V2 operator /(float s, V2 v) { return v / s; }

    public static float operator *(V2 a, V2 b) { return a.x * b.x + a.y * b.y; } // dot product

    public float Len2() { return x * x + y * y; }
    public float Len() { return Math.Sqrt(Len2()); }

    public void Normalize() {

    }
    public V2 GetNormalized() {

    }

    public V2 GetRotated(float angle) {
        float sin = Math.Sin(angle);
        float cos = Math.Cos(angle);
        return GetRotated(sin, cos);
    }
    public V2 GetRotated(float sin, float cos) {
        return new V2 {
            x = cos * x - sin * y,
            y = sin * x + cos * y,
        };
    }

    public void Rotate(float angle) {

    }
    public void Rotate(float sin, float cos) {

    }
}

public class Body {
    public V2 center;
    public float angle;
    public float invMass; // = 1 / mass
    //TODO: inertia
    public V2 velLin;
    public float velRot;

    public void Integrate(float dt) {
        center += velLin * dt;
        angle += velRot * dt;
    }

    public void ApplyImpulse(V2 impulse) {
        velLin += impulse * invMass;
    }
}
public class BodyCircle : Body {
    public float r;
}
public class BodyPolygon : Body {
    public List<V2> corners;

    public List<V2> GetGlobalCorners() {
        
    }
}

public class Main : MonoBehaviour {

    List<BodyCircle> circles = new List<BodyCircle> {
        
    };
    List<BodyPolygon> polys = new List<BodyPolygon> {
        new BodyPolygon {
            center = new V2 {
                x = 1,
                y = 0,
            },
            angle = Math.PI * 0.25f,
            invMass = 1f,
            velLin = new V2 {
                x = -0.7f,
                y = 0.3f,
            },
            velRot = Math.PI * 2f,

            corners = new List<V2> {
                new V2 { x = 1, y = 1 },
                new V2 { x = 1, y = -1 },
                new V2 { x = -1, y = -1 },
                new V2 { x = -1, y = 1 },
            }
        }
    };
    
    BodyCircle outerCircle = new BodyCircle {
        r = 0.5f,
        center = new V2(),
        velLin = new V2 { x = 1f, y = 1f },
        invMass = 5f,
    };

    void Start () {

        RenderInit();

        const int N = 20;
        for(int i = 0; i < N; i++) {
            float angle = 2f * Math.PI * i / N;
            float r = Random.Range(0.2f, 0.4f);
            circles.Add(new BodyCircle {
                center = new V2 { x = Math.Cos(angle), y = Math.Sin(angle) } * r,
                velLin = new V2 { x = -Math.Cos(angle), y = -Math.Sin(angle) },
                r = r * 0.1f,
                invMass = 1f / (r * r),
            });
        }

        ColDetect(polys[0], null);
	}
    
	void Update () {

        // ------- Physics ----------

        float dt = Time.deltaTime;

        dt *= 0.3f;

        foreach(var i in circles) {
            i.Integrate(dt);
        }
        foreach(var i in polys) {
            i.Integrate(dt);
        }
        outerCircle.Integrate(dt);
        
        int n = circles.Count;
        for(int i = n - 1; i >= 0; i--) {
            var a = circles[i];
            for(int j = i - 1; j >= 0; j--) {
                var b = circles[j];
                HandleCirclesOutside(a, b);
            }
        }
        
        for(int i = n - 1; i >= 0; i--) {
            var o = circles[i];
            var outer = outerCircle;
            HandleCirclesInside(o, outer);
        }

        // ------ Render ----------

        RenderPre();

        DrawCircle(outerCircle.center.x, outerCircle.center.y, outerCircle.r, new Color(0, 0, 1, 0.5f));
        foreach(var i in circles) {
            DrawCircle(i.center.x, i.center.y, i.r, Color.green);
        }
        foreach(var i in polys) {
            var ps = new List<V2>();
            foreach(var c in i.corners) {
                var p = c.getRotated(i.angle);
                p += i.center;
                ps.Add(p);
            }
            DrawConvexPolygon(ps, new Color(0, 1, 0, 0.5f));
        }
        
        //DrawArrow(
        //    new V2 { x = -0.3f, y = -0.9f },
        //    new V2 { x = -0.7f, y = 0.3f },
        //    0.1f,
        //    Color.white );

        //int N = 50;
        //for(int i = 0; i < N; i++) {
        //    float ratio = (float)i / (float)N;
        //    float angle = 2 * Math.PI * ratio;
        //    V2 p = new V2 { x = 0.5f, y = 0.5f };
        //    V2 r = p.getRotated(angle);
        //    DrawCircle(r.x, r.y, 0.05f, new Color(0, ratio, 1 - ratio));
        //}

        RenderPost();
    }

    static void HandleCirclesInside(BodyCircle inner, BodyCircle outer) {
        float dr = outer.r - inner.r;
        V2 dp = inner.center - outer.center;
        float d2 = dp.Len2();
        bool areOverlapping = d2 >= dr * dr;
        if(areOverlapping) {
            float totInvMass = outer.invMass + inner.invMass;
            float invMassRatioA = outer.invMass / totInvMass;
            float invMassRatioB = inner.invMass / totInvMass;

            // eliminate overlapping (separation)
            float dist = Math.Sqrt(d2);
            float totOverlap = dist - dr;
            V2 normal = dp / -dist;
            inner.center += normal * (totOverlap * invMassRatioB);
            outer.center -= normal * (totOverlap * invMassRatioA);

            V2 dv = inner.velLin - outer.velLin;
            bool movingTowardsEachOther = dp * dv > 0;
            if(movingTowardsEachOther) { // bounce
                const float BOUNCINESS = 0.99f; // [0, 1]
                float desiredDv = -BOUNCINESS * (dv * normal);
                float desiredDvChange = desiredDv - (dv * normal);

                float dva = 1 * outer.invMass;
                float dvb = -1 * inner.invMass;
                float dvChange = dvb - dva;
                float impLen = desiredDvChange / dvChange;
                outer.ApplyImpulse(impLen * normal);
                inner.ApplyImpulse(-impLen * normal);
            }
        }
    }

    static void HandleCirclesOutside(BodyCircle a, BodyCircle b) {
        float tr = a.r + b.r;
        V2 dp = b.center - a.center;
        float d2 = dp.Len2();
        bool collided = d2 <= tr * tr;
        if(collided) {
            V2 dv = b.velLin - a.velLin;

            float totInvMass = a.invMass + b.invMass;
            float invMassRatioA = a.invMass / totInvMass;
            float invMassRatioB = b.invMass / totInvMass;

            // eliminate overlapping (separation)
            float dist = Math.Sqrt(d2);
            float totOverlap = tr - dist;
            V2 normal = dp / dist;
            b.center += normal * (totOverlap * invMassRatioB);
            a.center -= normal * (totOverlap * invMassRatioA);

            bool movingTowardsEachOther = dp * dv < 0;
            if(movingTowardsEachOther) { // bounce
                const float BOUNCINESS = 0.99f; // [0, 1]
                float desiredDv = -BOUNCINESS * (dv * normal);
                float desiredDvChange = desiredDv - (dv * normal);

                float dva = 1 * a.invMass;
                float dvb = -1 * b.invMass;
                float dvChange = dvb - dva;
                float impLen = desiredDvChange / dvChange;
                a.ApplyImpulse(impLen * normal);
                b.ApplyImpulse(-impLen * normal);
            }
        }
    }

    static Col ColDetect(BodyPolygon a, BodyPolygon b) {
        // calc world poses of all corners
        int na = a.corners.Count;
        var aps = new List<V2>();
        for(int i = 0; i < na; i++) {
            V2 p = a.corners[i];
            p.getRotated(a.angle);
            p += a.center;
            aps.Add(p);
        }

        int nb = b.cor

        int n = a.corners.Count;
        for(int i = 0, lastIndex = n - 1; i < n; lastIndex = i, i++) {
            V2 corner0 = a.corners[lastIndex];
            V2 corner1 = a.corners[i];

            V2 normal = new V2 {
                x = corner1.y - corner0.y,
                y = corner0.x - corner1.x,
            };
            normal /= normal.Len(); // TO-OPT: most of the time, unnecessary

            float aMin = 
        }
        return null;
    }

    class Col {
        public V2 pos;
        public V2 normal;
        public float depth;
    }

    // -------------- Render --------------------
    // thx to Omer Faruk Sayilir

    Mesh renderMesh;
    Material renderMaterial;
    List<Vector3> renderVerts = new List<Vector3>();
    List<int> renderTris = new List<int>();
    List<Color> renderColors = new List<Color>();

    void RenderInit() {
        renderMesh = new Mesh { name = "MyMesh" };
        renderMesh.MarkDynamic();
        renderMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    void RenderPre() {
        renderVerts.Clear();
        renderTris.Clear();
        renderColors.Clear();
    }

    void RenderPost() {
        renderMesh.SetVertices(renderVerts);
        renderMesh.SetTriangles(renderTris, 0);
        renderMesh.SetColors(renderColors);

        Graphics.DrawMesh(renderMesh, Matrix4x4.identity, renderMaterial, 0);
    }

    void DrawCircle(float x, float y, float r, Color color, int n = 100) {
        for(int i = 0; i < n; i++) {
            float angle = Mathf.PI * 2f * ((float)i / (float)n);
            renderVerts.Add(new Vector3 {
                x = x + Mathf.Cos(angle) * r,
                y = y + Mathf.Sin(angle) * r,
                z = 0f,
            });

            renderColors.Add(color);
        }

        int lastVert = renderVerts.Count - 1;
        for(int i = 2; i < n; i++) {
            renderTris.Add(lastVert);
            renderTris.Add(lastVert - n + i);
            renderTris.Add(lastVert - n + i - 1);
        }
    }

    void DrawConvexPolygon(List<V2> ps, Color color) {
        int n = ps.Count;
        for(int i = 0; i < n; i++) {
            renderVerts.Add(Convert(ps[i]));
            renderColors.Add(color);
        }

        int lastVert = renderVerts.Count - 1;
        for(int i = 2; i < n; i++) {
            renderTris.Add(lastVert);
            renderTris.Add(lastVert - n + i);
            renderTris.Add(lastVert - n + i - 1);
        }
    }

    void DrawArrow(V2 from, V2 to, float w, Color color) {
        V2 dp = to - from;
        float dpLen = dp.Len();
        V2 dir = dp / dpLen;
        V2 perp = new V2 { x = dir.y, y = -dir.x };
        V2 cornerHorDp = perp * w;

        const float HEAD_LEN_RATIO = 0.3f;

        V2 corner00 = from + cornerHorDp;
        V2 corner01 = from - cornerHorDp;
        V2 corner1Center = from + (dp * (1f - HEAD_LEN_RATIO));
        V2 corner10 = corner1Center + cornerHorDp;
        V2 corner11 = corner1Center - cornerHorDp;

        const float HEAD_WITH_RATIO = 2f;

        V2 headTop = to;
        V2 headBottom0 = corner1Center + cornerHorDp * HEAD_WITH_RATIO;
        V2 headBottom1 = corner1Center - cornerHorDp * HEAD_WITH_RATIO;

        renderVerts.Add(Convert(corner00));
        renderVerts.Add(Convert(corner01));
        renderVerts.Add(Convert(corner10));
        renderVerts.Add(Convert(corner11));
        renderVerts.Add(Convert(headTop));
        renderVerts.Add(Convert(headBottom0));
        renderVerts.Add(Convert(headBottom1));

        int atVert = renderVerts.Count - 1;
        renderTris.Add(atVert--);
        renderTris.Add(atVert--);
        renderTris.Add(atVert--);

        renderTris.Add(atVert);
        renderTris.Add(atVert - 1);
        renderTris.Add(atVert - 2);

        renderTris.Add(atVert - 1);
        renderTris.Add(atVert - 2);
        renderTris.Add(atVert - 3);

        for(int i = 0; i < 7; i++) renderColors.Add(color);
    }

    static Vector3 Convert(V2 v) { return new Vector3(v.x, v.y, 0f); }
}
