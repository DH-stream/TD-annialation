using UnityEngine;

namespace TDAnnihilation
{
    /// <summary>
    /// Animator-free villager prototype. Builds a simple humanoid from primitives and
    /// animates the joints directly, so it is independent of the current FBX/Avatar setup.
    /// Intended as a clean visual/animation proof before introducing a final skinned mesh.
    /// </summary>
    public sealed class GreenwardVillagerPrototype : MonoBehaviour
    {
        [SerializeField] private float height = 1.75f;
        [SerializeField] private bool buildOnAwake = true;

        private Transform body;
        private Transform head;
        private Transform leftArm;
        private Transform rightArm;
        private Transform leftLeg;
        private Transform rightLeg;
        private Transform torso;
        private bool built;
        private Vector3 lastPosition;
        private float gait;

        private Material skin;
        private Material tunic;
        private Material trousers;
        private Material boots;
        private Material hair;
        private Material belt;

        private void Awake()
        {
            if (buildOnAwake) Build();
        }

        public void Build()
        {
            if (built) return;
            built = true;
            body = new GameObject("VillagerBody").transform;
            body.SetParent(transform, false);

            skin = MakeMaterial("Villager Skin", new Color(.67f, .42f, .27f));
            tunic = MakeMaterial("Villager Tunic", new Color(.22f, .36f, .29f));
            trousers = MakeMaterial("Villager Trousers", new Color(.16f, .18f, .20f));
            boots = MakeMaterial("Villager Boots", new Color(.20f, .11f, .07f));
            hair = MakeMaterial("Villager Hair", new Color(.10f, .065f, .045f));
            belt = MakeMaterial("Villager Belt", new Color(.34f, .20f, .09f));

            float s = height / 1.75f;
            torso = Part("Torso", PrimitiveType.Capsule, new Vector3(0, 1.03f, 0), new Vector3(.42f, .58f, .27f) * s, tunic);
            var beltPart = Part("Belt", PrimitiveType.Cylinder, new Vector3(0, .91f, 0), new Vector3(.30f, .045f, .30f) * s, belt);
            beltPart.transform.localScale = new Vector3(.34f, .055f, .34f) * s;

            head = Part("Head", PrimitiveType.Sphere, new Vector3(0, 1.73f, 0), new Vector3(.29f, .31f, .29f) * s, skin).transform;
            var hairCap = Part("Hair", PrimitiveType.Sphere, new Vector3(0, 1.84f, -.01f), new Vector3(.30f, .18f, .30f) * s, hair);
            hairCap.transform.localScale = new Vector3(.31f, .17f, .31f) * s;

            leftArm = Limb("LeftArm", new Vector3(-.43f, 1.13f, 0), new Vector3(.14f, .50f, .14f) * s, tunic);
            rightArm = Limb("RightArm", new Vector3(.43f, 1.13f, 0), new Vector3(.14f, .50f, .14f) * s, tunic);
            Limb("LeftHand", new Vector3(-.43f, .82f, 0), new Vector3(.13f, .17f, .13f) * s, skin);
            Limb("RightHand", new Vector3(.43f, .82f, 0), new Vector3(.13f, .17f, .13f) * s, skin);

            leftLeg = Limb("LeftLeg", new Vector3(-.18f, .47f, 0), new Vector3(.16f, .52f, .16f) * s, trousers);
            rightLeg = Limb("RightLeg", new Vector3(.18f, .47f, 0), new Vector3(.16f, .52f, .16f) * s, trousers);
            Limb("LeftBoot", new Vector3(-.18f, .16f, .06f), new Vector3(.18f, .16f, .30f) * s, boots);
            Limb("RightBoot", new Vector3(.18f, .16f, .06f), new Vector3(.18f, .16f, .30f) * s, boots);

            lastPosition = transform.position;
        }

        private void Update()
        {
            if (!built) return;
            float speed = Vector3.ProjectOnPlane(transform.position - lastPosition, Vector3.up).magnitude / Mathf.Max(Time.deltaTime, .0001f);
            lastPosition = transform.position;
            float moving = Mathf.Clamp01(speed / 1.2f);
            float target = moving > .08f ? 1f : 0f;
            gait = Mathf.MoveTowards(gait, target, Time.deltaTime * 5f);

            float t = Time.time;
            float walk = Mathf.Sin(t * 8.5f) * gait;
            float bob = Mathf.Abs(Mathf.Sin(t * 8.5f)) * .018f * gait;
            body.localPosition = new Vector3(0, bob, 0);
            leftArm.localRotation = Quaternion.Euler(walk * 32f, 0, 0);
            rightArm.localRotation = Quaternion.Euler(-walk * 32f, 0, 0);
            leftLeg.localRotation = Quaternion.Euler(-walk * 28f, 0, 0);
            rightLeg.localRotation = Quaternion.Euler(walk * 28f, 0, 0);

            if (gait < .1f)
            {
                float breathe = Mathf.Sin(t * 2.2f) * .012f;
                torso.localScale = new Vector3(1f, 1f + breathe, 1f);
                head.localRotation = Quaternion.Euler(Mathf.Sin(t * .8f) * 2f, Mathf.Sin(t * .55f) * 5f, 0);
                leftArm.localRotation *= Quaternion.Euler(0, 0, Mathf.Sin(t * 1.1f) * 2f);
                rightArm.localRotation *= Quaternion.Euler(0, 0, -Mathf.Sin(t * 1.1f) * 2f);
            }
            else
            {
                head.localRotation = Quaternion.Euler(Mathf.Sin(t * 4.25f) * 2f, 0, 0);
            }
        }

        private Transform Limb(string name, Vector3 position, Vector3 scale, Material material)
        {
            return Part(name, PrimitiveType.Capsule, position, scale, material).transform;
        }

        private GameObject Part(string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(body, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return go;
        }

        private static Material MakeMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name };
            material.color = color;
            return material;
        }
    }
}
