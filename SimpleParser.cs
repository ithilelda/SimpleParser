using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using XiaWorld;

namespace FunctionalPatches.SimpleParser
{
    public class SimpleParser<T> where T : EventArgs
    {
        public delegate MemberExpression ExpBuilder(ParameterExpression param);

        public static Dictionary<string, ExpBuilder> unitBuilder;
        public static Dictionary<string, Type> enumTypes;
        public static Dictionary<string, int> precedence;

        static SimpleParser()
        {
            unitBuilder = new Dictionary<string, ExpBuilder>();
            unitBuilder["To"] = BuildTo;
            unitBuilder["From"] = BuildFrom;
            unitBuilder["Element"] = BuildElement;
            unitBuilder["Source"] = BuildSource;

            enumTypes = new Dictionary<string, Type>();
            enumTypes["Element"] = typeof(g_emElementKind);
            enumTypes["Source"] = typeof(g_emDamageSource);

            precedence = new Dictionary<string, int>();
            precedence["!"] = 2;
            precedence["&"] = 1;
            precedence["|"] = 1;
        }

        public Func<T,bool> Parse(string exp)
        {
            // first, we have to strip white spaces.
            var space_pattern = new Regex(@"\s+", RegexOptions.Compiled);
            var slim_exp = space_pattern.Replace(exp, "");
            // then we tokenize the slim string.
            var tokens = new SimpleTokenizer().Tokenize(slim_exp);
            // then we construct the parameter expression.
            var param = Expression.Parameter(typeof(T), "e");
            // then we construct the nodes.
            var expression = ParseTree(tokens, param);
            // then we construct the lambda.
            var lambda = Expression.Lambda<Func<T, bool>>(expression, param);
            // finally, we return the compiled delegate.
            return lambda.Compile();
        }

        private Expression ParseTree(IEnumerable<string> tokens, ParameterExpression param)
        {
            var queue = ShuntingYard(tokens);
            return ConstructBody(queue, param);
        }

        // the shunting yard algorithm that transforms the expression into a queue that's useful.
        private Queue<string> ShuntingYard(IEnumerable<string> tokens)
        {
            var iter = tokens.GetEnumerator();
            var output = new Queue<string>();
            var ops = new Stack<string>();
            while (iter.MoveNext())
            {
                var current = iter.Current;
                // if we encountered an open parenthesis, we simply push it onto the ops stack.
                if (current == "(")
                {
                    ops.Push(current);
                }
                // if we encountered a close parenthesis, we pop every operator from the stack until we find an open parenthesis.
                else if (current == ")")
                {
                    while(ops.Peek() != "(")
                    {
                        output.Enqueue(ops.Pop());
                    }
                    // if pop and peek doesn't throw an exception, we have successfully found an open parenthesis before the stack empties. So we discard the open parenthesis.
                    ops.Pop();
                }
                // if we encountered the operators.
                else if (precedence.ContainsKey(current))
                {
                    // while there is still an operator at the top of the ops stack.
                    // we pop the operator from the ops stack to the output queue only if:
                    // 1. the operator is not an open parenthesis.
                    // 2. the operator at the top of the stack has higher precedence.
                    while (ops.Count > 0 && ops.Peek() != "(" && precedence[ops.Peek()] > precedence[current])
                    {
                       output.Enqueue(ops.Pop());
                    }
                    // after poping thing that matters, we push the current operator onto the stack.
                    ops.Push(current);
                }
                // finally, if we found a unit, we simply push it to the output queue.
                else
                {
                    output.Enqueue(current);
                }
            }
            // finally, we pop everything that's left in the stack to the output queue.
            while(ops.Count > 0)
            {
                output.Enqueue(ops.Pop());
            }
            return output;
        }
        private Expression ConstructBody(Queue<string> queue, ParameterExpression param)
        {
            var exps = new Stack<Expression>();
            while(queue.Count > 0)
            {
                var current = queue.Dequeue();
                // if we get the unary operator, then we only pop one operand and construct the not expression from it.
                if (current == "!")
                {
                    var exp = Expression.Not(exps.Pop());
                    exps.Push(exp);
                }
                // not too much different than above, just popping two operands.
                else if (current == "&")
                {
                    var exp = Expression.And(exps.Pop(), exps.Pop());
                    exps.Push(exp);
                }
                else if (current == "|")
                {
                    var exp = Expression.Or(exps.Pop(), exps.Pop());
                    exps.Push(exp);
                }
                //if the current item is a unit.
                else
                {
                    // we parse the unit and push it to the expression stack.
                    exps.Push(ParseUnit(current, param));
                }
            }
            return exps.Pop();
        }

        private Expression ParseUnit(string token, ParameterExpression param)
        {
            var items = token.Split(new char[] { ':' });
            var name = items[0]; // because I didn't remove empty entries, name is always going to be there.
            var builder = unitBuilder[name];
            if (builder == null) throw new ArgumentException($"the token '{token}' has an invalid name!");
            var member = builder(param);
            ConstantExpression const_exp = null;
            if (items.Length > 1) // meaning that we can access the second item without error.
            {
                var constant = items[1];
                // if there's actually anything.
                if (!string.IsNullOrEmpty(constant))
                {
                    // if we can find the name in the enumTypes dictionary, then it means this constant must be an enum.
                    if (enumTypes.ContainsKey(name))
                    {
                        var enum_type = enumTypes[name];
                        var value = Enum.Parse(enum_type, constant);
                        const_exp = Expression.Constant(value);
                    }
                    // otherwise, it must be a number.
                    else
                    {
                        var value = int.Parse(constant);
                        const_exp = Expression.Constant(value);
                    }
                }
            }
            // if there is no constant expression to evaluate for, we just return the member access expression to get the value (this happens when some member is a boolean already).
            if (const_exp == null)
            {
                return member;
            }
            // otherwise, we do the equality evaluation and return.
            else
            {
                return Expression.Equal(member, const_exp);
            }
        }
        private static MemberExpression BuildTo(ParameterExpression param)
        {
            var target = Expression.Field(param, "Target"); // we access the Target field of our event arg e.
            return Expression.Property(target, "ID");
        }
        private static MemberExpression BuildFrom(ParameterExpression param)
        {
            var from = Expression.Field(param, "From"); // we access the From field of our event arg e.
            return Expression.Property(from, "ID");
        }
        private static MemberExpression BuildElement(ParameterExpression param)
        {
            return Expression.Field(param, "Element");
        }
        private static MemberExpression BuildSource(ParameterExpression param)
        {
            return Expression.Field(param, "Source");
        }
    }
}
