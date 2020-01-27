using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace FunctionalPatches.SimpleParser
{
    public class SimpleParser<T> where T : EventArgs
    {
        public ParameterExpression Param { get; private set; }
        public LanguageTable<T> Table { get; private set; }
        public SimpleTokenizer<T> Tokenizer { get; private set; }
        public SimpleParser()
        {
            Param = Expression.Parameter(typeof(T), "e");
            Table = new LanguageTable<T>(Param);
            Tokenizer = new SimpleTokenizer<T>(Table);
        } 

        public Func<T,bool> Parse(string exp)
        {
            // first, we have to strip white spaces.
            var space_pattern = new Regex(@"\s+", RegexOptions.Compiled);
            var slim_exp = space_pattern.Replace(exp, "");
            // then we tokenize the slim string.
            var tokens = Tokenizer.Tokenize(slim_exp);
            // then we use shunting yard to convert infix notation to reverse polish notation.
            var rpn = ShuntingYard(tokens);
            // then we construct the expression body.
            var expression = ConstructBody(rpn);
            // then we construct the lambda.
            var lambda = Expression.Lambda<Func<T, bool>>(expression, Param);
            // finally, we return the compiled delegate.
            return lambda.Compile();
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
                if (!string.IsNullOrEmpty(current))
                {
                    // if we encountered an open parenthesis, we simply push it onto the ops stack.
                    if (current == "(")
                    {
                        ops.Push(current);
                    }
                    // if we encountered a close parenthesis, we pop every operator from the stack until we find an open parenthesis.
                    else if (current == ")")
                    {
                        while (ops.Peek() != "(")
                        {
                            output.Enqueue(ops.Pop());
                        }
                        // if pop and peek doesn't throw an exception, we have successfully found an open parenthesis before the stack empties. So we discard the open parenthesis.
                        ops.Pop();
                    }
                    // if we encountered the operators.
                    else if (Table.Operators.ContainsKey(current))
                    {
                        // while there is still an operator at the top of the ops stack.
                        // we pop the operator from the ops stack to the output queue only if:
                        // 1. the operator is not an open parenthesis.
                        // 2. the operator at the top of the stack has higher precedence.
                        // 3. this operator is left associative and the operator at the top of the stack has same precedence.
                        while (ops.Count > 0 && ops.Peek() != "(")
                        {
                            var stack_op = Table.Operators[ops.Peek()];
                            var current_op = Table.Operators[current];
                            if ((current_op.IsLeftAssociative && stack_op.Precedence == current_op.Precedence) || stack_op.Precedence > current_op.Precedence)
                            {
                                output.Enqueue(ops.Pop());
                            }
                            else break; // we only perform the action if the condition is true. If we find an operator that doesn't meet the criteria, we stop and break the loop.
                        }
                        // after poping thing that matters, we push the current operator onto the stack.
                        // If anything went wrong above, we'll just let it throw exceptions and the program will later handle it.
                        ops.Push(current);
                    }
                    // finally, if we found a unit, we simply push it to the output queue.
                    else
                    {
                        output.Enqueue(current);
                    }
                }
            }
            // finally, we pop everything that's left in the stack to the output queue.
            while(ops.Count > 0)
            {
                output.Enqueue(ops.Pop());
            }
            return output;
        }
        private Expression ConstructBody(Queue<string> input)
        {
            var exps = new Stack<StackNode>();
            while(input.Count > 0)
            {
                var current = input.Dequeue();
                KLog.Log(KLogLevel.Debug, $"The current queued token is: {current}");
                // if we get an operator, we use the builder function stored in our language table to construct the expression, and push the expression back on top of the stack.
                if (Table.Operators.ContainsKey(current))
                {
                    var builder = Table.Operators[current].Builder;
                    var exp = builder(exps);
                    exps.Push(exp);
                }
                // a token starting with a dollar sign means that it is the root accessor, we need to construct the root accessor expression from it, and push it to the stack.
                else if (current.StartsWith("$"))
                {
                    var name = current.Substring(1);
                    var type = typeof(T).GetField(name).FieldType.AssemblyQualifiedName;
                    var exp = Expression.Field(Param, name);
                    exps.Push(new StackNode { Raw = type, Exp = exp });
                }
                //if the current item is a unit, we push it verbatim onto the stack as the Raw field of StackNode.
                else
                {
                    exps.Push(new StackNode { Raw = current });
                }
            }
            return exps.Pop().Exp;
        }
    }
}
