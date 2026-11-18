using UnityEngine;

namespace TorreIngenieria.Core
{
    /// <summary>Uso principal de un objeto de la mochila.</summary>
    public enum ItemCategory
    {
        Heal,
        Ball,
        Key,
        Battle,

        /// <summary>Apuntes (máquina técnica, Doc §10): enseñan <see cref="ItemData.moveId"/> y se gastan.</summary>
        Teach,

        /// <summary>Cafeína (caramelo raro, Doc §10): sube un nivel a un Pokémon.</summary>
        LevelUp,

        /// <summary>Arepa (revivir, Doc §10): revive a un debilitado con la mitad de sus PS.</summary>
        Revive,
    }

    /// <summary>
    /// Objeto del prototipo (Tinto, Agua de panela, Pokébola...). Los assets se generan desde la tabla
    /// del Editor (<c>PrototypeDataBuilder</c>) y se leen por id con <see cref="ItemDatabase"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "Torre Ingeniería/Datos/Objeto")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("Id en minúscula; coincide con el nombre del asset y con GameState.AddItem.")]
        public string itemId;

        public string displayName;

        public ItemCategory category;

        [Tooltip("PS que restaura (solo Heal).")]
        public int healAmount;

        [Tooltip("Multiplicador del ratio de captura (solo Ball).")]
        public float catchBonus = 1f;

        [Tooltip("Movimiento que enseña (solo Teach: los Apuntes).")]
        public string moveId;

        [Tooltip("Precio de compra en créditos; 0 si no se vende.")]
        public int price;

        [TextArea]
        public string description;

        [Tooltip("Icono de 16×16 px para la Mochila (Assets/Art/UI/Objetos/<id>.png); vacío mientras no exista.")]
        public Sprite icon;
    }
}
