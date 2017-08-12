using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Math {
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
}

public struct V2 {
    public float x;
    public float y;

    public static V2 operator +(V2 a, V2 b) { return new V2 { x = a.x + b.x, y = a.y + a.y }; }
    public static V2 operator -(V2 a, V2 b) { return new V2 { x = a.x - b.x, y = a.y - a.y }; }
    public static V2 operator *(V2 v, float s) { return new V2 { x = v.x * s, y = v.y * s }; }
    public static V2 operator *(float s, V2 v) { return v * s; }
    public static V2 operator /(V2 v, float s) { return new V2 { x = v.x / s, y = v.y / s }; }
    public static V2 operator /(float s, V2 v) { return v / s; }

    public float len2() { return x * x + y * y; }
    public float len() { return Math.Sqrt(len2()); }
}

public class Body {
    public float center, size, vel;
    public float invMass; // = 1 / mass

    public void ApplyImpulse(float amount) {
        vel += amount * invMass;

        V2 a = new V2();
        V2 b = new V2();
        a += b;

        a *= 2f;
        b = 2f * a;
    }
}

public class Main : MonoBehaviour {

    List<Body> bodies = new List<Body> {
        new Body {
            center = -2.4f,
            size = 2f,
            vel = 0f,
            invMass = 0f,
        },
        new Body {
            center = 2.4f,
            size = 2f,
            vel = -0f,
            invMass = 0f,
        },
    };

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


	void Start () {
        for(float i = 0; i <= 100f; i++) {
            print(i + ": " + Math.Sqrt(i));
        }

		for(int i = 0; i < 15; i++) {
            var nb = new Body {
                center = Random.Range(-1f, 1f),
                size = Random.Range(0.02f, 0.2f),
                vel = Random.Range(-0.3f, 0.3f),
            };
            nb.invMass = 1f / nb.size;
            bodies.Add(nb);
        }
	}
    
	void Update () {

        // ------- Physics ----------

        float dt = Time.deltaTime;

        foreach(var i in bodies) {
            i.center += i.vel * dt;
        }

        int n = bodies.Count;
        for(int i = n - 1; i >= 0; i--) {
            var a = bodies[i];
            for(int j = i - 1; j >= 0; j--) {
                var b = bodies[j];

                float totSize = a.size + b.size;
                float dp = b.center - a.center;
                float dpAbs = Mathf.Abs(dp);
                bool collided = dpAbs <= (totSize * 0.5f);
                if(collided) {
                    float dv = b.vel - a.vel;
                    bool movingTowardsEachOther = dp * dv < 0;
                    if(movingTowardsEachOther) {
                        float totInvMass = a.invMass + b.invMass;
                        float invMassRatioA = a.invMass / totInvMass;
                        float invMassRatioB = b.invMass / totInvMass;

                        // eliminate overlapping (separation)
                        float totOverlap = (totSize * 0.5f) - dpAbs;
                        float dpSign = dp / dpAbs;
                        b.center += totOverlap * invMassRatioB * dpSign;
                        a.center -= totOverlap * invMassRatioA * dpSign;

                        // bounce
                        const float BOUNCINESS = 0.99f; // [0, 1]
                        float desiredDv = -BOUNCINESS * dv;
                        float desiredDvChange = desiredDv - dv;
                        
                        float dva = 1 * a.invMass;
                        float dvb = -1 * b.invMass;
                        float dvChange = dvb - dva;
                        float imp = desiredDvChange / dvChange;
                        a.ApplyImpulse(imp);
                        b.ApplyImpulse(-imp);

                        // algebraic method
                        //float va = a.vel;
                        //float vb = b.vel;
                        //a.vel = -a.invMass * va + 2f * a.invMass * vb + b.invMass * va;
                        //a.vel /= totInvMass;
                        //b.vel = -b.invMass * vb + 2f * b.invMass * va + a.invMass * vb;
                        //b.vel /= totInvMass;
                    }
                }
            }
        }

        // ------ Rendering ----------
        rectsAt = 0;
        foreach(var i in bodies) {
            DrawRect(i.center, 0f, i.size, 1f);
        }

        // destroy unused rects
        while(rects.Count > rectsAt) {
            Destroy(rects[rects.Count - 1]);
            rects.RemoveAt(rects.Count - 1);
        }
    }

    void DrawRect(float centerX, float centerY, float widht, float height) {
        var rect = GetPooledRect();
        rect.transform.position = new Vector3(centerX, centerY);
        rect.transform.localScale = new Vector3(widht, height, 1f);
    }
}
