/*
OneLoneCoder - C# DataFile v1.00
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
A C# adaptation of an "easy to use" serialisation/deserialisation class 
that yields human readable hierachical files.

License (OLC-3)
~~~~~~~~~~~~~~~

Copyright 2024 Joshua Sigona <sigonasr2@gmail.com>

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:

1. Redistributions or derivations of source code must retain the above
copyright notice, this list of conditions and the following disclaimer.

2. Redistributions or derivative works in binary form must reproduce
the above copyright notice. This list of conditions and the following
disclaimer must be reproduced in the documentation and/or other
materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
contributors may be used to endorse or promote products derived
from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

Original Source
~~~~~~
https://github.com/OneLoneCoder/olcPixelGameEngine/blob/master/utilities/olcUTIL_DataFile.h

Original Author
~~~~~~
David Barr, aka javidx9, �OneLoneCoder 2019, 2020, 2021, 2022, 2023, 2024

*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

class DataFile
{
    public DataFile() { }

    // Sets the String Value of a Property (for a given index)
    public void SetString(String str, int item = 0)
    {
        while (content.Count<=item)
        {
            content.Add("");
        }
        content[item]=str;
    }
    // Retrieves the String Value of a Property (for a given index) or ""
    public String GetString(int item = 0)
    {
        if (item>=content.Count)
        {
            return BLANK;
        }
        return content[item];
    }
    // Retrieves the String Value of a Property, to include all values that are normally separated by the list separator.
    public String GetFullString()
    {
        String accumulatedStr = "";
        for (int i = 0; i<content.Count; i++)
        {
            if (accumulatedStr.Length>0) accumulatedStr+=", ";
            accumulatedStr+=content[i];
        }
        return accumulatedStr;
    }

    // Retrieves the Real Value of a Property (for a given index) or 0.0
    public double GetReal(int item = 0)
    {
        return double.Parse(content[item]);
    }
    // Sets the Real Value of a Property (for a given index)
    public void SetReal(double d, int item = 0)
    {
        SetString(d.ToString(), item);
    }
    // Retrieves the Integer Value of a Property (for a given index) or 0
    public int GetInt(int item = 0)
    {
        return int.Parse(content[item]);
    }
    // Sets the Integer Value of a Property (for a given index)
    public void SetInt(int n, int item = 0)
    {
        SetString(n.ToString(), item);
    }
    // Retrieves the Boolean Value of a Property (for a given index) or false
    public bool GetBool(int item = 0)
    {
        return bool.Parse(content[item]);
    }
    // Sets the Boolean Value of a Property
    public void SetBool(bool b, int item = 0)
    {
        SetString(b.ToString(), item);
    }
    public int GetValueCount()
    {
        return content.Count;
    }
    public ref List<String> GetValues()
    {
        return ref content;
    }
    public ref Dictionary<String, int> GetKeys()
    {
        return ref mapObjects;
    }
    public ref List<Tuple<String, DataFile>> GetOrderedKeys()
    {
        return ref objects;
    }

    // Checks if a property exists - useful to avoid creating properties
    // via reading them, though non-essential
    public bool HasProperty(String name)
    {
        int x = name.IndexOf('.');
        if (x!=-1)
        {
            String property = name.Substring(0, x);
            if (HasProperty(property))
            {
                return GetProperty(name.Substring(0, x)).HasProperty(name.Substring(x+1, name.Length));
            }
            return false;
        }
        return mapObjects.ContainsKey(name);
    }

    // Access a datafile via a convenient name - "root.node.something.property"
    public DataFile GetProperty(String name)
    {
        int x = name.IndexOf(".");
        if (x!=-1)
        {
            String property = name.Substring(0, x);
            if (HasProperty(property))
            {
                return this[property].GetProperty(name.Substring(x+1, name.Length-(x+1)));
            }
            return this[property];
        }
        return this[name];
    }
    // Access a numbered element - "node[23]", or "root[56].node"
    public DataFile GetIndexedProperty(String name, int index)
    {
        return GetProperty(name+"["+index.ToString()+"]");
    }

    // Writes a "datafile" node (and all of its child nodes and properties) recursively
    // to a file.
    public static bool Write(ref DataFile n, String fileName, String sIndent = "\t", char listSep = ',')
    {
        // Cache indentation level
        int indentCount = 0;
        // Cache separator string for convenience
        String separator = listSep+" ";

        // Fully specified lambda, because this lambda is recursive!
        Action<DataFile, FileStream> write = null; //The lambda references itself! Therefore it must be declared earlier!
        write = (DataFile n, FileStream file) =>
        {
            Func<String, int, String> indent = (String str, int count) =>
            {
                String sOut = "";
                for (int n = 0; n<count; n++) sOut+=str;
                return sOut;
            };

            Action<String> writeToFile = str =>
            {
                byte[] data = new UTF8Encoding(true).GetBytes(str);
                file.Write(data, 0, data.Length);
            };

            foreach (Tuple<String, DataFile> property in n.objects)
            {
                // Does property contain any sub objects?
                if (property.Item2.objects.Count==0)
                {
                    // No, so it's an assigned field and should just be written. If the property
                    // is flagged as comment, it has no assignment potential. First write the 
                    // property name
                    writeToFile(indent(sIndent, indentCount)+property.Item1+(property.Item2.isComment ? "" : " = "));

                    // Second, write the property value (or values, seperated by provided
                    // separation charater
                    int items = property.Item2.GetValueCount();
                    for (int i = 0; i<property.Item2.GetValueCount(); i++)
                    {
                        // If the Value being written, in string form, contains the separation
                        // character, then the value must be written inside quotation marks. Note, 
                        // that if the Value is the last of a list of Values for a property, it is
                        // not suffixed with the separator
                        int x = property.Item2.GetString(i).IndexOf(listSep);
                        if (x!=-1)
                        {
                            // Value contains separator, so wrap in quotes
                            writeToFile("\""+property.Item2.GetString(i)+"\""+((items>1) ? separator : ""));
                        }
                        else
                        {
                            writeToFile(property.Item2.GetString(i)+((items>1) ? separator : ""));
                        }
                        items--;
                    }

                    // Property written, move to next line
                    writeToFile("\n");
                }
                else
                {
                    // Yes, property has properties of its own, so it's a node
                    // Force a new line and write out the node's name
                    writeToFile("\n"+indent(sIndent, indentCount)+property.Item1+"\n");
                    // Open braces, and update indentation
                    writeToFile(indent(sIndent, indentCount)+"{\n");
                    indentCount++;
                    // Recursively write that node
                    write(property.Item2, file);
                    // Node written, so close braces
                    writeToFile(indent(sIndent, indentCount)+"}\n\n");
                }
            }

            // We've finished writing out a node, regardless of state, our indentation
            // must decrease, unless we're top level
            if (indentCount>0) indentCount--;
        };

        // Start Here! Open the file for writing
        using (FileStream fs = File.Create(fileName))
        {
            write(n, fs);
            return true;
        };
    }

    public static bool Read(ref DataFile n, String fileName, char listSep = ',')
    {
        using (StreamReader sr = new StreamReader(fileName))
        {
            // These variables are outside of the read loop, as we will
            // need to refer to previous iteration values in certain conditions
            String propName = "";
            String propValue = "";

            // The file is fundamentally structured as a stack, so we will read it
            // in a such, but note the data structure in memory is not explicitly
            // stored in a stack, but one is constructed implicitly via the nodes
            // owning other nodes (aka a tree)
            Stack<DataFile> stkPath = new Stack<DataFile>();
            stkPath.Push(n);

            // Read file line by line and process
            String line = sr.ReadLine();
            while (line!=null)
            {
                //Support functions that behave like the C++ equivalents.
                Func<String, String, int?> firstNotOf = (String source, String chars) =>
                {
                    return source.Select((x, i) => new { Val = x, Idx = (int?)i })
                    .Where(x => chars.IndexOf(x.Val) == -1)
                    .Select(x => x.Idx)
                    .FirstOrDefault();
                };
                Func<String, String, int?> lastNotOf = (String source, String chars) =>
                {
                    return source.Select((x, i) => new { Val = x, Idx = (int?)i })
                    .Where(x => chars.IndexOf(x.Val) == -1)
                    .Select(x => x.Idx)
                    .LastOrDefault();
                };

                // This little lambda removes whitespace from
                // beginning and end of supplied string
                Func<String, String> trim = s =>
                {
                    int? firstWhiteSpace = firstNotOf(s, " \t\n\r\f\v");
                    if (firstWhiteSpace.HasValue)
                    {
                        s=s.Substring(firstWhiteSpace.Value);
                    }
                    int? lastWhiteSpace = lastNotOf(s, " \t\n\r\f\v");
                    if (lastWhiteSpace.HasValue)
                    {
                        s=s.Substring(0, lastWhiteSpace.Value+1);
                    }
                    return s;
                };

                line=trim(line);

                // If line has content
                if (line.Length>0)
                {
                    // Test if its a comment...
                    if (line[0]=='#')
                    {
                        // ...it is a comment, so ignore
                        DataFile comment = new DataFile();
                        comment.isComment=true;
                        stkPath.First().objects.Add(new Tuple<String, DataFile>(line, comment));
                    }
                    else
                    {
                        // ...it is content, so parse. Firstly, find if the line
                        // contains an assignment. If it does then it's a property...
                        int x = line.IndexOf('=');
                        if (x!=-1)
                        {
                            // ...so split up the property into a name, and its values!

                            // Extract the property name, which is all characters up to
                            // first assignment, trim any whitespace from ends
                            propName=line.Substring(0, x);
                            propName=trim(propName);
                            DataFile top = stkPath.First();

                            // Extract the property value, which is all characters after
                            // the first assignment operator, trim any whitespace from ends
                            propValue=line.Substring(x+1, line.Length-(x+1));
                            propValue=trim(propValue);

                            // The value may be in list form: a, b, c, d, e, f etc and some of those
                            // elements may exist in quotes a, b, c, "d, e", f. So we need to iterate
                            // character by character and break up the value
                            bool inQuotes = false;
                            String token = "";
                            int tokenCount = 0;
                            foreach (char c in propValue)
                            {
                                // Is character a quote...
                                if (c=='\"')
                                {
                                    // ...yes, so toggle quote state
                                    inQuotes=!inQuotes;
                                }
                                else
                                {
                                    // ...no, so proceed creating token. If we are in quote state
                                    // then just append characters until we exit quote state.
                                    if (inQuotes)
                                    {
                                        token+=c;
                                    }
                                    else
                                    {
                                        // Is the character our seperator? If it is
                                        if (c==listSep)
                                        {
                                            // Clean up the token
                                            token=trim(token);
                                            // Add it to the vector of values for this property
                                            stkPath.First()[propName].SetString(token, tokenCount);
                                            // Reset our token state
                                            token="";
                                            tokenCount++;
                                        }
                                        else
                                        {
                                            // It isnt, so just append to token
                                            token+=c;
                                        }
                                    }
                                }
                            }

                            // Any residual characters at this point just make up the final token,
                            // so clean it up and add it to the vector of values
                            if (token.Length>0)
                            {
                                token=trim(token);
                                stkPath.First()[propName].SetString(token, tokenCount);
                            }
                        }
                        else
                        {
                            // ...but if it doesnt, then it's something structural
                            if (line[0]=='{')
                            {
                                // Open brace, so push this node to stack, subsequent properties
                                // will belong to the new node
                                stkPath.Push(stkPath.First()[propName]);
                            }
                            else
                            {
                                if (line[0]=='}')
                                {
                                    // Close brace, so this node has been defined, pop it from the
                                    // stack
                                    stkPath.Pop();
                                }
                                else
                                {
                                    // Line is a property with no assignment. Who knows whether this is useful,
                                    // but we can simply add it as a valueless property...
                                    propName=line;
                                    // ...actually it is useful, as valueless properties are typically
                                    // going to be the names of new datafile nodes on the next iteration
                                }
                            }
                        }
                    }
                }

                line=sr.ReadLine();
            }
        }
        return true;
    }

    public DataFile this[String name]
    {
        get
        {
            // Check if this "node"'s map already contains an object with this name...
            if (!mapObjects.ContainsKey(name))
            {
                // ...it did not! So create this object in the map. First get a vector id 
                // and link it with the name in the unordered_map
                mapObjects[name]=objects.Count;
                // then creating the new, blank object in the vector of objects
                objects.Add(new Tuple<String, DataFile>(name, new DataFile()));
            }

            // ...it exists! so return the object, by getting its index from the map, and using that
            // index to look up a vector element.
            return objects[mapObjects[name]].Item2;
        }
        set
        {
            Tuple<String, DataFile> data = objects[mapObjects[name]];
            objects[mapObjects[name]]=new Tuple<String, DataFile>(name, value);
        }
    }

    // Used to identify if a property is a comment or not, not user facing
    protected bool isComment = false;

    private const String BLANK = "";

    // The "list of strings" that make up a property value
    private List<String> content = new List<String>();

    // Linkage to create "ordered" unordered_map. We have a vector of
    // "properties", and the index to a specific element is mapped.
    private Dictionary<String, int> mapObjects = new Dictionary<String, int>();
    private List<Tuple<String, DataFile>> objects = new List<Tuple<String, DataFile>>();

}
