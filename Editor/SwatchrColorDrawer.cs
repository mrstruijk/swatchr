using UnityEditor;
using UnityEngine;


namespace swatchr
{
    [CustomPropertyDrawer(typeof(SwatchrColor))]
    public class SwatchrColorDrawer : PropertyDrawer
    {
        private Texture2D swatchTexture;
        private Texture2D palleteTexture;
        private int palleteTextureCachedHash;
        private Texture2D blackTexture;

        private bool paletteOpen;

        private GUIStyle tempDrawTextureStyle;


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var swatchrColor = (SwatchrColor) fieldInfo.GetValue(property.serializedObject.targetObject);
            var swatch = swatchrColor.swatch;
            var color = swatchrColor.color;

            if (swatchTexture == null)
            {
                #if SWATCHR_VERBOSE
				Debug.LogWarning("[swatchrColorDrawer] creating swatch texture");
                #endif
                swatchTexture = textureWithColor(color);
            }

            var swatchProperty = property.FindPropertyRelative("_swatch");
            var colorIndexProperty = property.FindPropertyRelative("_colorIndex");
            var overrideColorProperty = property.FindPropertyRelative("_overrideColor");

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var swatchSize = EditorGUIUtility.singleLineHeight;
            var keySize = EditorGUIUtility.singleLineHeight * 1.25f;
            var spacing = EditorGUIUtility.singleLineHeight * 0.5f;
            //var toggleSize				= EditorGUIUtility.singleLineHeight;
            var toggleSize = 0;
            var swatchObjectPositionX = swatch == null ? position.x : position.x + swatchSize + keySize + toggleSize + spacing * 2;

            //var swatchObjectWidth = swatch == null ? position.width : position.width - swatchSize - keySize - spacing * 2;
            var fullWidth = position.width - swatchObjectPositionX + position.x;
            var swatchObjectWidth = fullWidth;
            var colorWidth = 0.25f * fullWidth;

            if (swatch == null)
            {
                swatchObjectWidth *= 0.75f;
            }

            var swatchObjectRect = new Rect(swatchObjectPositionX, position.y, swatchObjectWidth, EditorGUIUtility.singleLineHeight);
            var swatchRect = new Rect(position.x, position.y, swatchSize, EditorGUIUtility.singleLineHeight);
            var colorIndexRect = new Rect(swatchRect.position.x + swatchRect.width + spacing, position.y, keySize, EditorGUIUtility.singleLineHeight);
            var colorField = new Rect(position.x + position.width - colorWidth, position.y, colorWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginProperty(position, label, property);


            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;


            // Draw Swatch object
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(swatchObjectRect, swatchProperty, GUIContent.none);

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                swatchrColor.swatch = swatchrColor._swatch; // hack which calls observer pattern
                UpdateActiveSwatch(swatchrColor.color);
            }

            if (swatch != null)
            {
                // Draw Color Field
                if (DrawTextureButton(swatchTexture, swatchRect))
                {
                    paletteOpen = !paletteOpen && swatch != null && swatch.colors != null && swatch.colors.Length > 0;
                }

                DrawBlackGrid(swatchRect.x, swatchRect.y, 1, 1, (int) EditorGUIUtility.singleLineHeight);

                // Draw Color index text field
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(colorIndexRect, colorIndexProperty, GUIContent.none);

                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                    swatchrColor.colorIndex = colorIndexProperty.intValue; // hack which calls observer pattern
                    UpdateActiveSwatch(swatchrColor.color);
                }
                // Draw Toggle
                //EditorGUI.PropertyField(usingSwatchGroupToggleR, usingSwatchGroupProperty, GUIContent.none);
                //usingSwatchGroupProperty.boolValue = EditorGUI.Toggle(usingSwatchGroupToggleR, usingSwatchGroupProperty.boolValue);

                if (paletteOpen)
                {
                    var swatchHash = swatch.cachedTexture.GetHashCode();

                    if (palleteTexture == null || palleteTextureCachedHash != swatchHash)
                    {
                        #if SWATCHR_VERBOSE
						Debug.LogWarning("[swatchrColorDrawer] creating pallete texture");
                        #endif
                        palleteTexture = textureWithColors(swatch.colors);
                        palleteTextureCachedHash = swatchHash;
                    }

                    var textureRect = new Rect(swatchRect.x, swatchRect.y + EditorGUIUtility.singleLineHeight + 3, palleteTexture.width * EditorGUIUtility.singleLineHeight, palleteTexture.height * EditorGUIUtility.singleLineHeight);
                    DrawTexture(palleteTexture, textureRect);
                    DrawBlackGrid(textureRect.x, textureRect.y, palleteTexture.width, palleteTexture.height, (int) EditorGUIUtility.singleLineHeight);

                    // listen to click
                    var e = Event.current;

                    if (IsClickInRect(textureRect))
                    {
                        var rectClickPosition = e.mousePosition - textureRect.position;
                        var cellXIndex = (int) (rectClickPosition.x / EditorGUIUtility.singleLineHeight);
                        var cellYIndex = (int) (rectClickPosition.y / EditorGUIUtility.singleLineHeight);
                        var colorIndex = cellYIndex * palleteTexture.width + cellXIndex;
                        colorIndexProperty.intValue = colorIndex;
                        property.serializedObject.ApplyModifiedProperties();
                        swatchrColor.colorIndex = colorIndex; //  calls observer pattern
                        UpdateActiveSwatch(swatchrColor.color);
                    }
                    else if (IsClick())
                    {
                        paletteOpen = false;
                        EditorUtility.SetDirty(property.serializedObject.targetObject); // Repaint
                    }
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(colorField, overrideColorProperty, GUIContent.none);

                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                    swatchrColor.colorIndex = colorIndexProperty.intValue; // hack which calls observer pattern
                }
            }

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var originalHeight = base.GetPropertyHeight(property, label);

            if (!paletteOpen || palleteTexture == null)
            {
                return originalHeight;
            }

            return originalHeight + palleteTexture.height * EditorGUIUtility.singleLineHeight + 5;
        }


        private Texture2D textureWithColor(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGB24, false, true);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixel(0, 0, color);
            tex.Apply();

            return tex;
        }


        private Texture2D textureWithColors(Color[] colors)
        {
            var itemsPerRow = 5;
            // figure out our texture size based on the itemsPerRow and color count
            var totalRows = Mathf.CeilToInt(colors.Length / (float) itemsPerRow);
            var tex = new Texture2D(itemsPerRow, totalRows, TextureFormat.RGBA32, false, true);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;
            var x = 0;
            var y = 0;

            for (var i = 0; i < colors.Length; i++)
            {
                x = i % itemsPerRow;
                y = totalRows - 1 - Mathf.CeilToInt(i / itemsPerRow);
                tex.SetPixel(x, y, colors[i]);
            }

            for (x++; x < tex.width; x++)
            {
                tex.SetPixel(x, y, Color.clear);
            }

            tex.Apply();

            return tex;
        }


        private void DrawBlackGrid(float startingPointX, float startingPointY, int cellsX, int cellsY, int cellSize)
        {
            if (blackTexture == null)
            {
                #if SWATCHR_VERBOSE
				Debug.LogWarning("[swatchrColorDrawer] creating black texture");
                #endif
                blackTexture = textureWithColor(Color.black);
            }

            // draw vertical lines
            var currentRect = new Rect(startingPointX, startingPointY, 1, cellSize * cellsY);

            for (var i = 0; i <= cellsX; i++)
            {
                currentRect.x = startingPointX + cellSize * i;
                DrawTexture(blackTexture, currentRect);
            }

            currentRect.x = startingPointX;
            currentRect.height = 1;
            currentRect.width = cellSize * cellsX;

            for (var i = 0; i <= cellsY; i++)
            {
                currentRect.y = startingPointY + cellSize * i;

                if (i == cellsY)
                {
                    currentRect.width++;
                }

                DrawTexture(blackTexture, currentRect);
            }
        }


        private void DrawTexture(Texture2D texture, Rect rect)
        {
            if (tempDrawTextureStyle == null)
            {
                tempDrawTextureStyle = new GUIStyle();
            }

            tempDrawTextureStyle.normal.background = texture;
            EditorGUI.LabelField(rect, "", tempDrawTextureStyle);
        }


        private bool DrawTextureButton(Texture2D texture, Rect rect)
        {
            var buttonPressed = GUI.Button(rect, "", GUIStyle.none);
            DrawTexture(texture, rect);

            return buttonPressed;
        }


        private void UpdateActiveSwatch(Color color)
        {
            swatchTexture.SetPixel(0, 0, color);
            swatchTexture.Apply();
            SwatchEditorGUI.GameViewRepaint();
        }


        private bool IsClick()
        {
            var e = Event.current;

            return e != null && e.type == EventType.MouseDown && e.button == 0;
        }


        private bool IsClickInRect(Rect rect)
        {
            var e = Event.current;

            return e != null && e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition);
        }
    }
}