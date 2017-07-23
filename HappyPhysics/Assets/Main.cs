using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Body {
    public float center, size, vel;
    public float invMass; // = 1 / mass

    public void ApplyImpulse(float amount) {
        vel += amount * invMass;
    }
}

public class Main : MonoBehaviour {

    List<Body> bodies = new List<Body> {
        new Body {
            center = -1f,
            size = 0.2f,
            vel = 0.3f,
            invMass = 1f,
        },
        new Body {
            center = 1f,
            size = 0.2f,
            vel = -0.3f,
            invMass = 1f,
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
                bool collided = Mathf.Abs(dp) <= (totSize * 0.5f);
                if(collided) {
                    float dv = b.vel - a.vel;
                    bool movingTowardsEachOther = dp * dv < 0;
                    if(movingTowardsEachOther) {
                        float totInvMass = a.invMass + b.invMass;
                        float invMassRatioA = a.invMass / totInvMass;
                        float invMassRatioB = b.invMass / totInvMass;

                        // eliminate overlapping (separation)
                        float totOverlap = (totSize * 0.5f) - Mathf.Abs(dp);
                        b.center += totOverlap * invMassRatioB * dp / Mathf.Abs(dp);
                        a.center -= totOverlap * invMassRatioA * dp / Mathf.Abs(dp);

                        // bounce
                        const float BOUNCINESS = 0.1f; // [0, 1]
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
