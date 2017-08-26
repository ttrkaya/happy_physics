using System.Collections;
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
}

public class Body {
    public V2 center;
    public V2 vel;
    public float r;
    public float invMass; // = 1 / mass

    public void ApplyImpulse(V2 impulse) {
        vel += impulse * invMass;
    }
}

public class Main : MonoBehaviour {

    // Render
    // thx to Omer Faruk Sayilir
    Mesh mesh;
    Material material;

    List<Vector3> verts = new List<Vector3>();
    List<int> tris = new List<int>();
    List<Color> colors = new List<Color>();

    // Physics

    List<Body> bodies = new List<Body> {
        
    };
    
    Body outerCircle = new Body {
        r = 0.5f,
        center = new V2(),
        vel = new V2 { x = 1f, y = 1f },
        invMass = 5f,
    };

    void Start () {
        mesh = new Mesh { name = "MyMesh" };
        mesh.MarkDynamic();

        Shader shader = Shader.Find("Sprites/Default");
        material = new Material(shader);

        const int N = 20;
        for(int i = 0; i < N; i++) {
            float angle = 2f * Math.PI * i / N;
            float r = Random.Range(0.2f, 0.4f);
            bodies.Add(new Body {
                center = new V2 { x = Math.Cos(angle), y = Math.Sin(angle) } * r,
                vel = new V2 { x = -Math.Cos(angle), y = -Math.Sin(angle) },
                r = r * 0.1f,
                invMass = 1f / (r * r),
            });
        }
	}
    
	void Update () {

        // ------- Physics ----------

        float dt = Time.deltaTime;

        dt *= 0.3f;

        foreach(var i in bodies) {
            i.center += i.vel * dt;
        }
        outerCircle.center += outerCircle.vel * dt;
        
        int n = bodies.Count;
        for(int i = n - 1; i >= 0; i--) {
            var a = bodies[i];
            for(int j = i - 1; j >= 0; j--) {
                var b = bodies[j];
                HandlePairOutside(a, b);
            }
        }
        
        for(int i = n - 1; i >= 0; i--) {
            var o = bodies[i];
            var outer = outerCircle;
            HandlePairInside(o, outer);
        }

        // ------ Rendering ----------

        RenderPre();

        DrawConvexPolygon(new List<Vector2> {
            new Vector2(1, 1),
            new Vector2(1, 2),
            new Vector2(3, 3),
            new Vector2(2, 1),
        }, 0f, Color.red);

        DrawCircle(outerCircle.center.x, outerCircle.center.y, 1f, outerCircle.r, Color.blue);
        foreach(var i in bodies) {
            DrawCircle(i.center.x, i.center.y, 0f, i.r, Color.green);
        }
        RenderPost();
    }

    private static void HandlePairInside(Body inner, Body outer) {
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

            V2 dv = inner.vel - outer.vel;
            bool movingTowardsEachOther = dp * dv > 0;
            if(movingTowardsEachOther) { // bounce
                const float BOUNCINESS = 0.99f; // [0, 1]
                float desiredDv = -BOUNCINESS * (dv * normal);
                float desiredDvChange = desiredDv - (dv * normal);

                float dva = 1 * outer.invMass;
                float dvb = -1 * inner.invMass;
                float dvChange = dvb - dva;
                float imp = desiredDvChange / dvChange;
                //a.ApplyImpulse(imp * normal);
                outer.vel += outer.invMass * (imp * normal);
                inner.ApplyImpulse(-imp * normal);
            }
        }
    }

    private static void HandlePairOutside(Body a, Body b) {
        float tr = a.r + b.r;
        V2 dp = b.center - a.center;
        float d2 = dp.Len2();
        bool collided = d2 <= tr * tr;
        if(collided) {
            V2 dv = b.vel - a.vel;

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
                float imp = desiredDvChange / dvChange;
                a.ApplyImpulse(imp * normal);
                b.ApplyImpulse(-imp * normal);
            }
        }
    }

    // Render

    public void RenderPre() {
        verts.Clear();
        tris.Clear();
        colors.Clear();
    }

    public void RenderPost() {
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colors);

        Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 0);
    }

    public void DrawCircle(float x, float y, float z, float r, Color color, int n = 100) {
        for(int i = 0; i < n; i++) {
            float angle = Mathf.PI * 2f * ((float)i / (float)n);
            float px = x + Mathf.Cos(angle) * r;
            float py = y + Mathf.Sin(angle) * r;
            verts.Add(new Vector3 {
                x = px,
                y = py,
                z = z,
            });

            colors.Add(color);
        }

        int lastVert = verts.Count - 1;
        for(int i = 2; i < n; i++) {
            tris.Add(lastVert);
            tris.Add(lastVert - n + i);
            tris.Add(lastVert - n + i - 1);
        }
    }

    public void DrawConvexPolygon(List<Vector2> ps, float z, Color color) {
        int n = ps.Count;
        for(int i = 0; i < n; i++) {
            var p = ps[i];
            verts.Add(new Vector3 {
                x = p.x,
                y = p.y,
                z = z,
            });
            colors.Add(color);
        }

        int lastVert = verts.Count - 1;
        for(int i = 2; i < n; i++) {
            tris.Add(lastVert);
            tris.Add(lastVert - n + i);
            tris.Add(lastVert - n + i - 1);
        }
    }
}
