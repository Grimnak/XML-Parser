using System;
using System.Collections.Generic;

public class Element
{
    private string _name = String.Empty;
    private List<Attribute> _attributes = new List<Attribute>();
    private bool _closed = false;
    private Stack<Element> _path = new Stack<Element>();
    private string _elementValue = String.Empty;
    private Element _parent = null;
    private List<Element> _children = new List<Element>();

    public string Name { get { return _name; } }
    public List<Attribute> Attributes { get { return _attributes; } }
    public bool Closed { get { return _closed; } set { _closed = value; } }
    public Stack<Element> Path { get { return _path; } set { _path = value; } }
    public string ElementValue { get { return _elementValue; } set { _elementValue = value; } }
    public Element Parent { get { return _parent; } set { _parent = value; } }
    public List<Element> Children { get { return _children; } set { _children = value; } }

    // Gather what information we can from the element string.
    public Element(string element)
    {
        int currentIndex = 0;
        int nameStartIndex = 0;

        // Iterate through the element string until we reach whitespace or a closing symbol.  This gives us the element name.
        while (!Char.IsWhiteSpace(element[currentIndex]) && !element[currentIndex].Equals('>') && !element[currentIndex].Equals('/'))
        {
            currentIndex++;
        }

        _name = element.Substring(nameStartIndex, currentIndex - nameStartIndex);

        while (Char.IsWhiteSpace(element[currentIndex]))
        {
            currentIndex++;
        }

        // If the next non-whitespace character after the element name is "/", then the element is self-closing and there are no attributes, so we're finished parsing this element.
        if (element[currentIndex].Equals('/'))
        {
            _closed = true;
        }
        // If the next non-whitespace character after the element name is ">", then there are no attributes, so we're finished parsing this element; otherwise, search for and parse any attributes.
        else if (!element[currentIndex].Equals('>'))
        {
            int attributeStartIndex = -1;
            bool withinSingleQuote = false;
            bool withinDoubleQuote = false;
            bool withinAttribute = false;

            // Identify attributes and add them to the attribute list until tag closing characters are found.
            while (!element[currentIndex].Equals('/'))
            {
                // Keep track of whether or not we're inside quotation marks.  Reaching the outermost quotation mark indicates we have reached the end of an attribute.
                if (element[currentIndex].Equals('"'))
                {
                    if (withinDoubleQuote && !withinSingleQuote)
                    {
                        withinAttribute = false;
                    }

                    withinDoubleQuote = !withinDoubleQuote;
                    currentIndex++;
                    continue;
                }
                else if (element[currentIndex].Equals('\''))
                {
                    if (withinSingleQuote && !withinDoubleQuote)
                    {
                        withinAttribute = false;
                    }

                    withinSingleQuote = !withinSingleQuote;
                    currentIndex++;
                    continue;
                }

                if (!withinDoubleQuote && !withinSingleQuote)
                {
                    // A ">" indicates that there will be no more attributes.
                    if (element[currentIndex].Equals('>'))
                    {
                        _attributes.Add(new Attribute(element.Substring(attributeStartIndex, currentIndex - attributeStartIndex)));
                        break;
                    }
                    // If whitespace is found and we're not currently within an attribute, we have reached the end of an attribute and can add it to the list.
                    else if (Char.IsWhiteSpace(element[currentIndex]))
                    {
                        if (!withinAttribute)
                        {
                            _attributes.Add(new Attribute(element.Substring(attributeStartIndex, currentIndex - attributeStartIndex)));
                            attributeStartIndex = -1;
                        }

                        currentIndex++;
                    }
                    // If we make it to this case, the character is the beginning of an attribute, so we need to save this index to add the attribute later.
                    else if (attributeStartIndex == -1)
                    {
                        withinAttribute = true;
                        attributeStartIndex = currentIndex;
                        currentIndex++;
                    }
                    else
                    {
                        currentIndex++;
                    }
                }
                else
                {
                    currentIndex++;
                }
            }
        }

        // If a "/" was located, then this is a self-closing element.
        if (element[currentIndex].Equals('/'))
        {
            _closed = true;
        }
    }

    // Once an element has been closed and there cannot be any more additions or changes during parsing, ensure that any escaped characters are replaced with their normal symbols.
    public void ReplaceEscapedCharacters()
    {
        // Escaped characters within the element value.
        if (!_elementValue.Equals(String.Empty))
        {
            _elementValue.Replace("&amp;", "&");
            _elementValue.Replace("&quot;", "\"");
            _elementValue.Replace("&apos;", "'");
            _elementValue.Replace("&lt;", "<");
            _elementValue.Replace("&gt;", ">");
        }

        // Escaped characters within the element attributes.
        foreach (Attribute attribute in _attributes)
        {
            attribute.ReplaceEscapedCharacters();
        }
    }

    // Return the calling element's path as a string.
    public string PrettyPrintElementPath()
    {
        string path = String.Empty;

        foreach (Element elem in _path)
        {
            path += "/";
            path += elem._name;
        }

        return path;
    }
}