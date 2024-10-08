﻿using UnityEngine;


namespace swatchr
{
    [RequireComponent(typeof(Light))]
    public class SwatchrLight : SwatchrColorApplier
    {
        private Light swatchingLight;


        protected override void Apply()
        {
            if (swatchingLight == null)
            {
                swatchingLight = GetComponent<Light>();
            }

            swatchingLight.color = swatchrColor.color;
        }
    }
}