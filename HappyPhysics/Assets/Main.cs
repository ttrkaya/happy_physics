using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Body {
    public float center, size, vel;
    public float invMass; // = 1 / mass
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
                    // bounce
                    float dv = b.vel - a.vel;
                    bool movingTowardsEachOther = dp * dv < 0;
                    if(movingTowardsEachOther) {
                        a.vel *= -1f;
                        b.vel *= -1f;
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
