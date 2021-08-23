using System;
using System.Collections.Generic;
using System.IO;

public class ParseXML
{
    // We are using a list to keep track of element hierarchy.  Each newly added element is a direct child of the most recently added open element on the list, if any open elements exist.
    private List<Element> elementList = new List<Element>();

    // The user navigates through the list element by element.
    private int currentPosition = 0;

    // This constructor performs the actual XML parsing.
    public ParseXML(string xmlFilePath)
    {
        try
        {
            using (StreamReader reader = new StreamReader(xmlFilePath))
            {
                string line;
                string completeElementTag = String.Empty;
                bool withinComment = false;
                bool withinTag = false;

                // Read line by line until the end of the XML file.
                while ((line = reader.ReadLine()) != null)
                {
                    int currentIndex = 0;

                    // Must account for multi-line element tags.
                ElementTagCheck:
                    if (!withinTag)
                    {
                        // Must account for multi-line XML comments.
                    CommentStatusCheck:
                        if (!withinComment)
                        {
                            int valueStartingIndex = currentIndex;

                            // Traverse to the next "<" of the line, if it exists; otherwise, read the next line.
                            while (currentIndex < line.Length && !line[currentIndex].Equals('<'))
                            {
                                currentIndex++;
                            }

                            // If an open element exists in the list, then add this text to the most recently added open element's value.
                            Element newestOpenElement = FindNewestOpenElement();

                            if (newestOpenElement != null)
                            {
                                newestOpenElement.ElementValue += line.Substring(valueStartingIndex, currentIndex - valueStartingIndex);
                            }

                            if (currentIndex >= line.Length)
                            {
                                continue;
                            }
                            else
                            {
                                currentIndex++;
                            }

                            // If "<" is immediately followed by "!", we're dealing with a comment (because of the constraint that we're provided with valid XML, we can assume the "!" is followed by "--").
                            if (line[currentIndex].Equals('!'))
                            {
                                withinComment = true;
                                currentIndex += 3;
                                goto CommentStatusCheck;
                            }
                            // If "<" is immediately followed by a letter or underscore, we are parsing a new element tag.
                            else if (Char.IsLetter(line[currentIndex]) || line[currentIndex].Equals('_'))
                            {
                                withinTag = true;
                                goto ElementTagCheck;
                            }
                            // If "<" is immediately followed by "/", we have a closing tag for an element already in the list.  Update its status to closed, its list of children, and replace escaped characters.
                            else if (line[currentIndex].Equals('/'))
                            {
                                currentIndex++;
                                Element elem = UpdateClosedStatus(line, currentIndex);

                                if (elem != null)
                                {
                                    UpdateChildren(elem);
                                    elem.ReplaceEscapedCharacters();
                                }
                            }

                            // This implementation does not handle other tags yet (e.g. prolog information, etc.).  Here we simply locate the corresponding ">" if it exists on this line.
                            while (currentIndex < line.Length && !line[currentIndex].Equals('>'))
                            {
                                currentIndex++;
                            }

                            if (currentIndex >= line.Length)
                            {
                                continue;
                            }
                            else
                            {
                                currentIndex++;
                                goto ElementTagCheck;
                            }
                        }
                        else
                        {
                            // We need to locate the corresponding comment ending "-->", which may or may not be on this line.
                            currentIndex = FindCommentEnd(line, currentIndex);

                            // The end of the comment wasn't on this line.
                            if (currentIndex < 0)
                            {
                                continue;
                            }
                            // The end of the comment was found so continue parsing.
                            else
                            {
                                withinComment = false;
                                goto CommentStatusCheck;
                            }
                        }
                    }
                    else
                    {
                        // We need to locate the corresponding tag ending character ">", which may or may not be on this line.
                        int tagEndIndex = FindTagEnd(line, currentIndex);

                        // The end of the tag wasn't on this line.
                        if (tagEndIndex < 0)
                        {
                            completeElementTag += line.Substring(currentIndex, line.Length - currentIndex);
                            continue;
                        }
                        // The end of the tag was found so add the element to the list and update pertinent information, then continue parsing.
                        else
                        {
                            withinTag = false;
                            completeElementTag += line.Substring(currentIndex, tagEndIndex - currentIndex);
                            Element newElement = new Element(completeElementTag);
                            UpdateHierarchyInfo(newElement);
                            elementList.Add(newElement);
                            completeElementTag = String.Empty;
                            currentIndex = tagEndIndex;
                            goto ElementTagCheck;
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Console.WriteLine("Couldn't complete file parsing:");
            Console.WriteLine(e.Message);
        }
    }

    // If we've identified that we're inside a tag, and return the index just after the tag ending if found (or return -1 if not found).
    private int FindTagEnd(string line, int currentIndex)
    {
        int startIndex = currentIndex;

        while (currentIndex < line.Length && !line[currentIndex].Equals('>'))
        {
            currentIndex++;
        }

        if (currentIndex >= line.Length)
        {
            return -1;
        }
        else
        {
            return ++currentIndex;
        }
    }

    // If we've identified that we're inside a comment, return the index just after the comment ending if found (or return -1 if not found).
    private int FindCommentEnd(string line, int currentIndex)
    {
        while (true)
        {
            while (currentIndex < line.Length && !line[currentIndex].Equals('-'))
            {
                currentIndex++;
            }

            if (currentIndex >= line.Length)
            {
                return -1;
            }
            else
            {
                currentIndex++;

                // XML requires two consecutive "-", otherwise we cannot assume we reached the end of a comment.
                if (currentIndex < line.Length && line[currentIndex].Equals('-'))
                {
                    // Increment by 2 because once two consecutive "-" are found, we know the next character is ">" given that we are always provided valid XML.
                    return currentIndex += 2;
                }
                else
                {
                    currentIndex++;
                }
            }
        }
    }

    // If we've identified a corresponding closing tag for an already-existing element in the list, update the appropriate element's closed status and return the element.
    private Element UpdateClosedStatus(string line, int currentIndex)
    {
        int nameStartIndex = currentIndex;

        // The name of the element to close continues until the ending ">" or whitespace is found.
        while (currentIndex < line.Length && !line[currentIndex].Equals('>') && !Char.IsWhiteSpace(line[currentIndex]))
        {
            currentIndex++;
        }

        // Search for the most recently added element in the list with the same name and update its closed status.
        if (currentIndex < line.Length)
        {
            string elementName = line.Substring(nameStartIndex, currentIndex - nameStartIndex);

            for (int index = elementList.Count - 1; index >= 0; index--)
            {
                if (elementList[index].Name.Equals(elementName) && !elementList[index].Closed)
                {
                    elementList[index].Closed = true;
                    return elementList[index];
                }
            }
        }

        return null;
    }

    // Once an element has been closed, populate its list of children given that it cannot possibly have any more added.
    private void UpdateChildren(Element closedElement)
    {
        for (int index = elementList.Count - 1; index >= 0; index--)
        {
            // To ensure speed, stop iterating through the list once we find the closed element in question because any elements added to the list before the original cannot possibly be its children.
            if (elementList[index].Equals(closedElement))
            {
                break;
            }
            else if (elementList[index].Parent.Equals(closedElement))
            {
                closedElement.Children.Add(elementList[index]);
            }
        }
    }

    // Once an element has been added to the list, immediately set its direct parent, if it has one, as well as the path to reach the element.
    private void UpdateHierarchyInfo(Element newElement)
    {
        for (int index = elementList.Count - 1; index >= 0; index--)
        {
            // The most recently added open element in the list must be the newly-added element's direct parent.
            if (!elementList[index].Closed)
            {
                newElement.Parent = elementList[index];
                break;
            }
        }

        Element element = newElement;

        // Follow the chain of elements until we reach a null parent value.
        while (element != null)
        {
            newElement.Path.Push(element);

            element = element.Parent;
        }
    }

    // Return the most recently added open element present in the list, if one exists.
    private Element FindNewestOpenElement()
    {
        for (int index = elementList.Count - 1; index >= 0; index--)
        {
            if (!elementList[index].Closed)
            {
                return elementList[index];
            }
        }

        return null;
    }

    // Return whether or not the position counter can be incremented.
    public bool NextElementExists()
    {
        return elementList.Count - currentPosition > 1;
    }

    // Return whether or not the position counter can be decremented.
    public bool PreviousElementExists()
    {
        return elementList.Count - currentPosition < elementList.Count;
    }

    // Return the path information corresponding to the current element in the list.
    public string GetCurrentElement()
    {
        ShowCurrentElement();
        return elementList[currentPosition].PrettyPrintElementPath();
    }

    // Reset the position counter and return the path information corresponding to the first element in the list.
    public string GetRootElement()
    {
        currentPosition = 0;
        ShowCurrentElement();
        return elementList[currentPosition].PrettyPrintElementPath();
    }

    // Advance the position counter and return the path information corresponding to the next element in the list if it exists or an empty string if it doesn't.
    public string GetNextElement()
    {
        if (NextElementExists())
        {
            currentPosition++;
            ShowCurrentElement();
            return elementList[currentPosition].PrettyPrintElementPath();
        }
        else
        {
            return String.Empty;
        }
    }

    // Advance the position counter and return the path information corresponding to the next element in the list with a specific name if it exists or an empty string if it doesn't.
    public string GetNextElement(string name)
    {
        do
        {
            if (NextElementExists())
            {
                currentPosition++;
            }
            else
            {
                // Element names can never be an empty string, so returning the empty string indicates the search failed.
                return String.Empty;
            }
        } while (!elementList[currentPosition].Name.Equals(name));

        ShowCurrentElement();
        return elementList[currentPosition].Name;
    }

    // Retreat the position counter and return the path information corresponding to the previous element in the list if it exists or an empty string if it doesn't.
    public string GetPreviousElement()
    {
        if (PreviousElementExists())
        {
            currentPosition--;
            ShowCurrentElement();
            return elementList[currentPosition].PrettyPrintElementPath();
        }
        else
        {
            return String.Empty;
        }
    }

    // Retreat the position counter and return the path information corresponding to the previous element in the list with a specific name if it exists or an empty string if it doesn't.
    public string GetPreviousElement(string name)
    {
        do
        {
            if (PreviousElementExists())
            {
                currentPosition--;
            }
            else
            {
                // Element names can never be an empty string, so returning the empty string indicates the search failed.
                return String.Empty;
            }
        } while (!elementList[currentPosition].Name.Equals(name));

        ShowCurrentElement();
        return elementList[currentPosition].Name;
    }

    // Return the attribute information corresponding to the current element in the list.
    public Dictionary<string, string> GetAttributeInfo()
    {
        Dictionary<string, string> attributeDictionary = new Dictionary<string, string>();

        foreach (Attribute attribute in elementList[currentPosition].Attributes)
        {
            attributeDictionary.Add(attribute.Name, attribute.AttributeValue);
        }

        return attributeDictionary;
    }

    // Return the children's names of the current element in the list.
    public List<string> GetChildrenNames()
    {
        List<string> childrenList = new List<string>();

        foreach (Element elem in elementList[currentPosition].Children)
        {
            childrenList.Add(elem.Name);
        }

        return childrenList;
    }

    // Display the value corresponding to the current element in the list.
    public string GetValue()
    {
        return elementList[currentPosition].ElementValue.Trim();
    }

    // Display information corresponding to the current element in the list.
    public void ShowCurrentElement()
    {
        Console.WriteLine("~~~~~~~~~~~~~~~");
        Console.WriteLine("Name:  " + elementList[currentPosition].Name);
        Console.WriteLine("Path:  " + elementList[currentPosition].PrettyPrintElementPath());

        if (elementList[currentPosition].ElementValue.Trim().Length > 0)
        {
            Console.WriteLine("Value:  " + elementList[currentPosition].ElementValue.Trim());
        }

        if (elementList[currentPosition].Attributes.Count > 0)
        {
            Console.WriteLine("Attributes:  ");
            foreach (Attribute attribute in elementList[currentPosition].Attributes)
            {
                Console.WriteLine("    Name:  " + attribute.Name);
                Console.WriteLine("    Value:  " + attribute.AttributeValue);
            }
        }

        if (elementList[currentPosition].Children.Count > 0)
        {
            Console.WriteLine("Children:  ");
            foreach (Element child in elementList[currentPosition].Children)
            {
                Console.WriteLine("    " + child.Name);
            }
        }

        Console.WriteLine("~~~~~~~~~~~~~~~");
    }

    // Reset the position counter and display information corresponding to the first element in the list.
    public void ShowRootElement()
    {
        currentPosition = 0;
        ShowCurrentElement();
    }

    // Advance the position counter and display information corresponding to the next element in the list.
    public void ShowNextElement()
    {
        if (NextElementExists())
        {
            currentPosition++;
            ShowCurrentElement();
        }
        else
        {
            Console.WriteLine("No next element exists.");
        }
    }

    // Retreat the position counter and display information corresponding to the previous element in the list.
    public void ShowPreviousElement()
    {
        if (PreviousElementExists())
        {
            currentPosition--;
            ShowCurrentElement();
        }
        else
        {
            Console.WriteLine("No previous element exists.");
        }
    }

    // Display the attribute information corresponding to the current element in the list.
    public void ShowAttributes()
    {
        foreach (Attribute attribute in elementList[currentPosition].Attributes)
        {
            Console.WriteLine(attribute.Name + " --- " + attribute.AttributeValue);
        }

        if (elementList[currentPosition].Attributes.Count == 0)
        {
            Console.WriteLine("No attributes correspond to the current element.");
        }
    }

    // Display the child information corresponding to the current element in the list.
    public void ShowChildren()
    {
        foreach (Element elem in elementList[currentPosition].Children)
        {
            Console.WriteLine(elem.Name);
        }

        if (elementList[currentPosition].Children.Count == 0)
        {
            Console.WriteLine("No children belong to the current element.");
        }
    }

    // Display the value corresponding to the current element in the list.
    public void ShowValue()
    {
        string trimmedElementValue = elementList[currentPosition].ElementValue.Trim();

        if (trimmedElementValue.Length > 0)
        {
            Console.WriteLine(trimmedElementValue);
        }
        else
        {
            Console.WriteLine("No value corresponds to the current element.");
        }
    }

    // Display detailed information about all XML elements and the entire hierarchy.
    public void ShowAllDocumentInfo()
    {
        foreach (Element elem in elementList)
        {
            Console.WriteLine("~~~~~~~~~~~~~~~");
            Console.WriteLine("Name:  " + elem.Name);
            Console.WriteLine("Path:  " + elem.PrettyPrintElementPath());

            if (elem.ElementValue.Trim().Length > 0)
            {
                Console.WriteLine("Value:  " + elem.ElementValue.Trim());
            }
            else
            {
                Console.WriteLine("No value.");
            }

            if (elem.Attributes.Count > 0)
            {
                Console.WriteLine("Attributes:  ");
                foreach (Attribute attribute in elem.Attributes)
                {
                    Console.WriteLine("    Name:  " + attribute.Name);
                    Console.WriteLine("    Value:  " + attribute.AttributeValue);
                }
            }
            else
            {
                Console.WriteLine("No attributes.");
            }

            if (elem.Children.Count > 0)
            {
                Console.WriteLine("Children:  ");
                foreach (Element child in elem.Children)
                {
                    Console.WriteLine("    " + child.Name);
                }
            }
            else
            {
                Console.WriteLine("No children.");
            }

            Console.WriteLine("~~~~~~~~~~~~~~~");
        }
    }
}