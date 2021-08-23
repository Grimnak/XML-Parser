using System;
using System.Collections.Generic;

public class Example
{
    static void Main(string[] args)
    {
        // Parse the provided XML document.
        ParseXML xml = new ParseXML(args[0]);
        
        // Print out detailed information about the parsed XML's elements and hierarchy.
        xml.ShowAllDocumentInfo();
        Console.WriteLine();

        // Print out all order IDs with an amount greater than 100 [ensure ExampleXML.xml was parsed].
        List<string> orderIDs = new List<string>();

        while (!xml.GetNextElement("order").Equals(String.Empty))
        {
            Dictionary<string, string> orderAttributes = xml.GetAttributeInfo();

            // Travel to each child element named "amount" and check the value before printing the order ID.
            foreach (string childName in xml.GetChildrenNames())
            {
                int value;

                if (xml.GetNextElement(childName).Equals("amount") && Int32.TryParse(xml.GetValue(), out value) && value > 100)
                {
                    foreach (KeyValuePair<string, string> attribute in orderAttributes)
                    {
                        if (attribute.Key.Equals("id"))
                        {
                            orderIDs.Add(attribute.Value);
                        }
                    }
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("Order IDs:");

        foreach (string id in orderIDs)
        {
            Console.WriteLine(id);
        }
    }
}