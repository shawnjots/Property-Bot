using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Azure.Search.Models;

namespace Microsoft.Bot.Sample.SearchDialogs
{
    public enum Operator { None, LessThan, LessThanOrEqual, Equal, GreaterThanOrEqual, GreaterThan, And, Or };

    [Serializable]
    public class FilterExpression
    {
        public readonly Operator Operator;
        public readonly object[] Values;

        public FilterExpression()
        { }

        public FilterExpression(Operator op, params object[] values)
        {
            Operator = op;
            Values = values;
        }

        public static string ToFilter(SearchField field, FilterExpression expression)
        {
            string filter = "";
            if (expression.Values.Length > 0)
            {
                var constant = Constant(expression.Values[0]);
                string op = null;
                bool connective = false;
                switch (expression.Operator)
                {
                    case Operator.LessThan: op = "lt"; break;
                    case Operator.LessThanOrEqual: op = "le"; break;
                    case Operator.Equal: op = "eq"; break;
                    case Operator.GreaterThan: op = "gt"; break;
                    case Operator.GreaterThanOrEqual: op = "ge"; break;
                    case Operator.Or: op = "or"; connective = true; break;
                    case Operator.And: op = "and"; connective = true; break;
                }
                if (connective)
                {
                    var builder = new StringBuilder();
                    var seperator = string.Empty;
                    builder.Append('(');
                    foreach(var child in expression.Values)
                    {
                        builder.Append(seperator);
                        builder.Append(ToFilter(field, (FilterExpression)child));
                        seperator = $" {op} ";
                    }
                    builder.Append(')');
                    filter = builder.ToString();
                }
                else
                {
                    if (field.Type == typeof(string[]))
                    {
                        if (expression.Operator != Operator.Equal)
                        {
                            throw new NotSupportedException();
                        }
                        filter = $"{field.Name}/any(z: z eq {constant})";
                    }
                    else
                    { 
                        filter = $"{field.Name} {op} {constant}";
                    }
                }
            }
            return filter;
        }

        public static string Constant(object value)
        {
            string constant = null;
            if (value is string)
            {
                constant = $"'{EscapeFilterString(value as string)}'";
            }
            else
            {
                constant = value.ToString();
            }
            return constant;
        }

        private static string EscapeFilterString(string s)
        {
            return s.Replace("'", "''");
        }

    }

    [Serializable]
    public class SearchQueryBuilder
    {
        private const int DefaultHitPerPage = 5;

        public SearchQueryBuilder()
        {
            this.Refinements = new Dictionary<string, FilterExpression>();
        }

        public string SearchText { get; set; }

        public int PageNumber { get; set; }

        public int HitsPerPage { get; set; } = DefaultHitPerPage;

        public Dictionary<string, FilterExpression> Refinements { get; private set; }

        public virtual SearchParameters BuildParameters()
        {
            SearchParameters parameters = new SearchParameters
            {
                Top = this.HitsPerPage,
                Skip = this.PageNumber * this.HitsPerPage,
                SearchMode = SearchMode.All
            };

            if (this.Refinements.Count > 0)
            {
                StringBuilder filter = new StringBuilder();
                string separator = string.Empty;

                foreach (var entry in this.Refinements)
                {
                    SearchField field;
                    if (SearchDialogIndexClient.Schema.Fields.TryGetValue(entry.Key, out field))
                    {
                        filter.Append(separator);
                        filter.Append(FilterExpression.ToFilter(field, entry.Value));
                        separator = " and ";
                    }
                    else
                    {
                        throw new ArgumentException($"{entry.Key} is not in the schema");
                    }
                }

                parameters.Filter = filter.ToString();
            }

            return parameters;
        }

        public virtual void Reset()
        {
            this.SearchText = null;
            this.PageNumber = 0;
            this.Refinements.Clear();
        }

    }
}
