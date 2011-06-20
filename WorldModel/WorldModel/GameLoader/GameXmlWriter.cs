﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace AxeSoftware.Quest
{
    internal class GameXmlWriter
    {
        private StringBuilder m_output;
        private XmlWriter m_writer;
        private int m_indentLevel = 0;
        private const string k_indentChars = "  ";
        private SaveMode m_mode;

        public GameXmlWriter(SaveMode mode)
        {
            m_mode = mode;
            m_output = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = k_indentChars;
            settings.OmitXmlDeclaration = true;
            m_writer = XmlWriter.Create(m_output, settings);
        }

        public void WriteComment(string text)
        {
            m_writer.WriteComment(text);
        }

        public void WriteStartElement(string localName)
        {
            m_writer.WriteStartElement(localName);
            m_indentLevel++;
        }

        public void WriteEndElement()
        {
            m_writer.WriteEndElement();
            m_indentLevel--;
        }

        public void WriteAttributeString(string localName, string value)
        {
            m_writer.WriteAttributeString(localName, value);
        }

        public void WriteString(string text)
        {
            // When writing a string containing newlines, the XmlWriter won't indent the ending tag at all.
            // So, add the right amount of indenting to the end of the string, so that the ending tag will
            // be indented correctly.
            if (text.Contains(Environment.NewLine))
            {
                for (int i = 0; i < m_indentLevel - 1; i++)
                {
                    text += k_indentChars;
                }
            }
            if (UseCDATA(text))
            {
                m_writer.WriteCData(text);
            }
            else
            {
                m_writer.WriteString(text);
            }
        }

        private bool UseCDATA(string input)
        {
            return input.Contains(">") || input.Contains("<") || input.Contains("&");
        }

        public void WriteElementString(string localName, string value)
        {
            m_writer.WriteElementString(localName, value);
        }

        public void Close()
        {
            m_writer.Close();
        }

        public override string ToString()
        {
            return m_output.ToString();
        }

        public int IndentLevel
        {
            get { return m_indentLevel; }
        }

        public string IndentChars
        {
            get { return k_indentChars; }
        }

        public SaveMode Mode
        {
            get { return m_mode; }
        }

    }
}
