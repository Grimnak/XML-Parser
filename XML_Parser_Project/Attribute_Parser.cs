using System;

public class Attribute
{
    private string _name = String.Empty;
    private string _attributeValue = String.Empty;

    public string Name { get { return _name; } set { _name = value; } }
    public string AttributeValue { get { return _attributeValue; } set { _attributeValue = value; } }

    // Gather what information we can from the attribute string.
    public Attribute(string attribute)
    {
        int currentIndex = 0;
        int valueStartIndex = 0;
        
        // Iterate through the attribute string until we reach whitespace or a "=".  This gives us the attribute name.
        while (!Char.IsWhiteSpace(attribute[currentIndex]) && !attribute[currentIndex].Equals('='))
        {
            currentIndex++;
        }

        _name = attribute.Substring(0, currentIndex);

        // Iterate through the attribute string until we reach quotation marks; within the quotes is the attribute value.
        while (!attribute[currentIndex].Equals('"') && !attribute[currentIndex].Equals('\''))
        {
            currentIndex++;
        }

        if (attribute[currentIndex].Equals('"'))
        {
            currentIndex++;
            valueStartIndex = currentIndex;

            while (!attribute[currentIndex].Equals('"'))
            {
                currentIndex++;
            }
        }
        else if (attribute[currentIndex].Equals('\''))
        {
            currentIndex++;
            valueStartIndex = currentIndex;

            while (!attribute[currentIndex].Equals('\''))
            {
                currentIndex++;
            }
        }

        _attributeValue = attribute.Substring(valueStartIndex, currentIndex - valueStartIndex);
    }

    // Once an element has been closed and there cannot be any more additions or changes during parsing, ensure that any escaped characters are replaced with their normal symbols.
    public void ReplaceEscapedCharacters()
    {
        // Escaped characters within the attribute value.
        if (!_attributeValue.Equals(String.Empty))
        {
            _attributeValue.Replace("&amp;", "&");
            _attributeValue.Replace("&quot;", "\"");
            _attributeValue.Replace("&apos;", "'");
            _attributeValue.Replace("&lt;", "<");
            _attributeValue.Replace("&gt;", ">");
        }
    }
}