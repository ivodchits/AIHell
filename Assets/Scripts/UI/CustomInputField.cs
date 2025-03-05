using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AIHell.UI
{
    public class CustomInputField : TMP_InputField
    {
        [SerializeField] int overridenCaretWidth = 15;
        [SerializeField] Color highlightedTextColor = Color.black;
        
        int startChar;
        int endChar;
        
        protected override void Awake()
        {
            base.Awake();
            caretWidth = overridenCaretWidth;
        }

        public override void Rebuild(CanvasUpdate update)
        {
            base.Rebuild(update);
            return;

            TMP_TextInfo textInfo = m_TextComponent.textInfo;
            
            if (stringPositionInternal != stringSelectPositionInternal)
            {
                m_CaretPosition = GetCaretPositionFromStringIndex(stringPositionInternal);
                m_CaretSelectPosition = GetCaretPositionFromStringIndex(stringSelectPositionInternal);

                if (m_SoftKeyboard != null)
                {
                    int stringPosition = m_CaretPosition < m_CaretSelectPosition
                        ? textInfo.characterInfo[m_CaretPosition].index
                        : textInfo.characterInfo[m_CaretSelectPosition].index;
                    int length = m_CaretPosition < m_CaretSelectPosition
                        ? stringSelectPositionInternal - stringPosition
                        : stringPositionInternal - stringPosition;
                    m_SoftKeyboard.selection = new RangeInt(stringPosition, length);
                }

                startChar = Mathf.Max(0, m_CaretPosition);
                endChar = Mathf.Max(0, m_CaretSelectPosition);

                // Ensure pos is always less then selPos to make the code simpler
                if (startChar > endChar)
                {
                    (startChar, endChar) = (endChar, startChar);
                }

                for (int i = startChar; i < endChar; i++)
                {
                    var copy = textInfo.characterInfo[i];
                    copy.color = highlightedTextColor;
                    textInfo.characterInfo[i] = copy;
                }
            }
            else
            {
                if (startChar != endChar)
                {
                    for (int i = startChar; i < endChar; i++)
                    {
                        var copy = textInfo.characterInfo[i];
                        copy.color = textComponent.color;
                        textInfo.characterInfo[i] = copy;
                    }
                
                    startChar = 0;
                    endChar = 0;
                }
            }
        }
        
        int GetCaretPositionFromStringIndex(int stringIndex)
        {
            int count = m_TextComponent.textInfo.characterCount;

            for (int i = 0; i < count; i++)
            {
                if (m_TextComponent.textInfo.characterInfo[i].index >= stringIndex)
                    return i;
            }

            return count;
        }
    }
}