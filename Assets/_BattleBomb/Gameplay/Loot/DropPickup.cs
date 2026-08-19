using UnityEngine;

namespace BattleBomb.Gameplay.Loot
{
    /// <summary>
    /// A dropped placeholder token (D23): spawned at the corpse when the roll pays out, free for
    /// whichever player reaches it first — the driver resolves grabs inside the fixed step, so a
    /// simultaneous couch dive has one deterministic winner. The token is abstract until M4's
    /// item generation gives quality something to mean; the quality rides along regardless.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DropPickup : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Color TokenColor = new Color(1f, 0.8f, 0.2f);

        private float _quality;

        public float Quality => _quality;

        public Vector3 Position => transform.position;

        internal static DropPickup Spawn(Vector3 corpse, float quality)
        {
            GameObject token = GameObject.CreatePrimitive(PrimitiveType.Cube);
            token.name = "Drop";
            Destroy(token.GetComponent<Collider>());
            token.transform.position = new Vector3(corpse.x, 0.35f, corpse.z);
            token.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            token.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

            MeshRenderer renderer = token.GetComponent<MeshRenderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, TokenColor);
            renderer.SetPropertyBlock(block);

            DropPickup pickup = token.AddComponent<DropPickup>();
            pickup._quality = quality;
            return pickup;
        }
    }
}
