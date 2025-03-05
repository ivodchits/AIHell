using System;
using UnityEngine;

namespace AIHell.UI
{
    [CreateAssetMenu(fileName = "ColorPalette", menuName = "ScriptableObjects/ColorPalette")]
    public class ColorPalette : ScriptableObject
    {
        [SerializeField] PaletteEntry[] paletteEntries;
        
        [Serializable]
        class PaletteEntry
        {
            public string name;
            public Color color;
        }
    }
}