using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Company.Common
{




    public class CsvModel
    {
        private Dictionary<String, Int32> _headers;
        private List<String> _headerItems;
        private List<List<String>> _data;
        public Boolean HasHeader { get; protected set; }
        public void SetData (List<List<String>> data, Boolean withHeaders = false)
        {
            _headerItems = null;
            _data = null;
            _headers = null;
            if (!data.Any())
                return;

            var f0 = data[0];
            _headers = new Dictionary<String, Int32>();
            
            if (withHeaders)
            {
                _headerItems = f0;
                _data = data.Skip(1).ToList();
                
            }
            else
            {
                _headerItems = Enumerable.Range(1, data[0].Count).Select(i=>i.ToString()).ToList();
                _data = data;
            }

            for (var i = 0; i < f0.Count; i++)
            {
                var h = f0[i];
                if (h != null && !_headers.ContainsKey(h))
                    _headers[h] = i;
            }
            
            
        }
        
        public List<String> Columns
        {
            get { return _headerItems; }
        }

        
        public Dictionary<String, String> this[int lineIndex]
        {
            get
            {
                var line = _data[lineIndex];
                var result = _headers.ToDictionary(it=>it.Key, it=> line[it.Value]);
                return result;
            }
        }

        public String this[String column, int lineIndex] {
            get
            {
                if (!_headers.ContainsKey(column))
                    return null;
                return _data[lineIndex][_headers[column]];
            }
            set { _data[lineIndex][_headers[column]] = value; }
        }

        public List<List<String>> Rows()
        {
            return _data;
        }

        public String this[Int32 columnIndex, Int32 lineIndex]
        {
            get { return _data[lineIndex][columnIndex]; }
            set { _data[lineIndex][columnIndex] = value; }
        }
        public Int32 Count { get { return _data.Count; } }
    
    }




    public static class CsvHelper
    {

        public static List<List<String>> LinesFromCsv(this String csvSource)
        {
            var result = new List<List<String>>();
            List<String> currentList = null;
            var v = csvSource.Trim(new char[] {' ', '\r', '\n', '\t'});
            while (true)
            {
                String field;
                Boolean endOfLine;
                v = GetField(v, out field, out endOfLine);
                if (field != null)
                {
                    if (currentList == null)
                    {
                        currentList = new List<string>();
                        result.Add(currentList);
                    }
                    currentList.Add(field);
                }
                
                if (v == null)
                    break;
                if (endOfLine)
                    currentList = null;
            }

            return result;

        }

        public static CsvModel FromCsv(this String csvSource, Boolean hasHeader = true)
        {

            var lines = LinesFromCsv(csvSource);
            var result = new CsvModel();

            if (lines.Count == 0)
            {
                return result;
            }
            var longest = lines.Max(it => it.Count);
            lines.ForEach(it =>
                {
                    if (it.Count<longest) it.AddRange(new String[longest-it.Count]);
                });
            result.SetData(lines, hasHeader);
            return result;
        }

        private static String GetField(String value, out String field, out bool endOfLine, String lineEnd = "\r\n")
        {
            field = null;
            endOfLine = true;
            if (String.IsNullOrEmpty(value))
                return null;
            var v = value.TrimStart(' ');
            var endIdx = v.Length - 1;

            if (v[0]=='"')
            {
                if (v.Length == 1)
                    throw new FormatException("Single double quote char is not allowed");
                var lastIndex = 1;
                while (true)
                {
                    var qIndex = v.IndexOf('"', lastIndex);
                    if (qIndex<0)
                        throw new FormatException("Closing double quote char is not found");
                    if (qIndex == endIdx || v[qIndex + 1] != '"')
                    {
                        field = v.Substring(1, qIndex - 1).Replace("\"\"","\"");
                        endIdx = qIndex + 1;
                        break;
                    }
                    if (v[qIndex + 1] == '"')
                        qIndex++;
                    lastIndex = qIndex + 1;
                    if (lastIndex == endIdx)
                        throw new FormatException("Closing double quote char is not found");
                }
            }
            else
            {
                endIdx = v.Length;
                var cIndex = v.IndexOf(',');
                var nIndex = v.IndexOf(lineEnd, StringComparison.Ordinal);
                if (nIndex >= 0)
                    endIdx = nIndex;
                if (cIndex >= 0 && cIndex < endIdx)
                    endIdx = cIndex;
                if (endIdx < 0)
                    return null;
                
                field = v.Substring(0, endIdx).Trim();
            }
            if (endIdx >= v.Length)
                return null;
            v = v.Substring(endIdx);
            v = v.TrimStart(' ');
            if (v.StartsWith(","))
            {
                v = v.Substring(1);
                endOfLine = false;
            }
            else if (v.StartsWith(lineEnd))
            {
                v = v.Substring(lineEnd.Length);
            }
            return v;
        }
    }
}