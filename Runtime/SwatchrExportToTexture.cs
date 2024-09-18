﻿using System.IO;
using UnityEngine;


namespace swatchr
{
    public static class SwatchrExportToTexture
    {
        public static void ExportSwatchToTexture(Swatch selectedSwatch, string fullSaveLocation)
        {
            var swatchrTexture = selectedSwatch.cachedTexture;
            var pngBytes = swatchrTexture.EncodeToPNG();
            Debug.Log("[SwatchrExportToTexture] exporting swatch to " + fullSaveLocation);
            File.WriteAllBytes(fullSaveLocation, pngBytes);
        }
    }
}