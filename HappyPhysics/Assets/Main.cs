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

    public GameObject prefabRect;
    List<GameObject> rects = new List<GameObject>();
    int rectsAt;
    GameObject GetPooledRect() {
        if(rectsAt == rects.Count) {
            GameObject newRect = Instantiate(prefabRect);
            rects.Add(newRect);
        }
        return rects[rectsAt++];
    }
    public GameObject prefabCircle;
    List<GameObject> circles = new List<GameObject>();
    int circleAt;
    GameObject GetPooledCircle() {
        if(circleAt == circles.Count) {
            GameObject newCircle = Instantiate(prefabCircle);
            circles.Add(newCircle);
        }
        return circles[circleAt++];
    }

    public Transform renderOuterCircle;

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

        renderOuterCircle.transform.position = new Vector3 {
            x = outerCircle.center.x,
            y = outerCircle.center.y,
            z = 1f,
        };

        rectsAt = 0;
        circleAt = 0;

        foreach(var i in bodies) {
            DrawCircle(i.center, i.r);
        }
        
        // destroy unused
        while(rects.Count > rectsAt) {
            Destroy(rects[rects.Count - 1]);
            rects.RemoveAt(rects.Count - 1);
        }
        while(circles.Count > circleAt) {
            Destroy(circles[circles.Count - 1]);
            circles.RemoveAt(circles.Count - 1);
        }
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

    void DrawRect(float centerX, float centerY, float widht, float height) {
        var rect = GetPooledRect();
        rect.transform.position = new Vector3(centerX, centerY);
        rect.transform.localScale = new Vector3(widht, height, 1f);
    }

    void DrawCircle(V2 center, float radius) {
        var circle = GetPooledCircle();
        circle.transform.position = new Vector3(center.x, center.y, 0f);
        float scale = radius * 2f;
        circle.transform.localScale = new Vector3(scale, scale, 0f);
    }
}
