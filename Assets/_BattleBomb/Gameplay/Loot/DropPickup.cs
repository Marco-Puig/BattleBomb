using BattleBomb.Core.Items;
using UnityEngine;

namespace BattleBomb.Gameplay.Loot
{
    /// <summary>
    /// A dropped item in the world (D23/D30): spawned at the corpse carrying its generated
    /// <see cref="ItemInstance"/>, tinted by quality, free for whichever player takes it first —
    /// the driver resolves grabs inside the fixed step, so a simultaneous couch dive has one
    /// deterministic winner.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DropPickup : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private ItemInstance _item;

        public ItemInstance Item => _item;

        public Vector3 Position => transform.position;

        internal static DropPickup Spawn(Vector3 corpse, in ItemInstance item)
        {
            GameObject token = GameObject.CreatePrimitive(PrimitiveType.Cube);
            token.name = "Drop";
            Destroy(token.GetComponent<Collider>());
            token.transform.position = new Vector3(corpse.x, 0.35f, corpse.z);
            token.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            token.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

            MeshRenderer renderer = token.GetComponent<MeshRenderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, QualityColors.For(item.Quality));
            renderer.SetPropertyBlock(block);

            DropPickup pickup = token.AddComponent<DropPickup>();
            pickup._item = item;
            return pickup;
        }
    }
}
