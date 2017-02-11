/*
 Copyright 2005 Queensland University of Technology (QUT). All rights reserved. Modified and improved by Tomas Matousek.

 Redistribution and use in source and binary forms, with or without modification are permitted provided 
 that the following conditions are met:
 
 Redistribution of source code must retain the above copyright notice, this list of conditions and 
 the following disclaimer. 
 
 Redistributions in binary form must reproduce the above copyright notice, this list of conditions 
 and the following disclaimer in the documentation and/or other materials with the distribution. 

 This software is provided by the GPPG project “as is” and any express or implied warranties, 
 including, but not limited to the implied warranties of merchantability and fitness for 
 a particular purpose are hereby disclaimed. In no event shall the GPPG project or QUT be liable 
 for any direct, indirect, incidental, special, exemplary, or consequential damages 
 (including, but not limited to procurement of substitute goods or services; 
 loss of use, data, or profits; or business interruption) however caused and on any theory of liability, 
 whether in contract, strict liability, or tort (including negligence or otherwise) 
 arising in any way out of the use of this software, even if advised of the possibility of such damage.

*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Pchp.Library.Parsers
{
	#region State, Rule

	public struct State
	{
		public int num;
		public readonly Dictionary<int, int> parser_table;  // Terminal -> ParseAction
		public readonly Dictionary<int, int> Goto;          // NonTerminal -> State;
		public readonly int defaultAction;			        // ParseAction
        
		private State(int num, int defaultAction, int[] actions, int[] gotos)
		{
            this.num = num;
            this.defaultAction = defaultAction;

            if (actions != null)
            {
                this.parser_table = new Dictionary<int, int>(actions.Length / 2);
                for (int i = 0; i < actions.Length; i += 2)
                    this.parser_table.Add(actions[i], actions[i + 1]);
            }
            else
                this.parser_table = null;

            if (gotos != null)
            {
                this.Goto = new Dictionary<int, int>(gotos.Length / 2);
                for (int i = 0; i < gotos.Length; i += 2)
                    this.Goto.Add(gotos[i], gotos[i + 1]);
            }
            else
                this.Goto = null;
		}

        public State(int num, int[] actions, int[] gotos)
            :this(num, 0, actions, gotos)
		{
		}

        public State(int num, int[] actions)
            : this(num, 0, actions, null)
        {
        }

		public State(int num, int defaultAction)
            : this(num, defaultAction, null, null)
		{
		}

        public State(int num, int defaultAction, int[] gotos)
            : this(num, defaultAction, null, gotos)
        {
        }
	}

	public struct Rule
	{
		public int lhs; // symbol
		public int[] rhs; // symbols

		public Rule(int lhs, int[] rhs)
		{
			this.lhs = lhs;
			this.rhs = rhs;
		}
	}

	#endregion

	#region ITokenProvider

	public interface ITokenProvider<ValueType, PositionType>
	{
		ValueType TokenValue { get; }

		PositionType TokenPosition { get; }

		int GetNextToken();

		void ReportError(string[] expectedTokens);
	}

	#endregion

	#region ParserStack

	public class ParserStack<ValueType, PositionType>
	{
		public struct Item
		{
			public ValueType yyval;
			public PositionType yypos;
			public bool yypos_valid;

			public override string ToString()
			{
				return yyval.ToString() + " " + yypos.ToString();
			}
		}

		// fields accessed from the generated code:
		public Item[] array = new Item[3];
		public int top = 0;

		public void Push(ValueType value, PositionType pos, bool isValidPosition)
		{
			if (top >= array.Length)
			{
				Item[] newarray = new Item[array.Length * 2];
				System.Array.Copy(array, newarray, top);
				array = newarray;
			}

			array[top].yyval = value;
			array[top].yypos = pos;
			array[top].yypos_valid = isValidPosition;
			top++;
		}

		public void Pop()
		{
			--top;
		}

		public ValueType PeekValue()
		{
			return array[top - 1].yyval;
		}

		public PositionType PeekPosition()
		{
			return array[top - 1].yypos;
		}

		public bool IsValidPosition()
		{
			return array[top - 1].yypos_valid;
		}
		
		public PositionType PeekPosition(int offset)
		{
			return array[top - 1 - offset].yypos;
		}

		public bool IsValidPosition(int offset)
		{
			return array[top - 1 - offset].yypos_valid;
		}
	}

	#endregion

	#region ShiftReduceParser

	public abstract class ShiftReduceParser<ValueType, PositionType>
		where ValueType : struct
	{
		//internal bool Trace = false;

		public ITokenProvider<ValueType, PositionType> Scanner { get { return scanner; } set { scanner = value; } }
		private ITokenProvider<ValueType, PositionType> scanner;

		protected ValueType yyval;
		protected PositionType yypos;
		protected bool yypos_valid;
		
		private int next;
		private int current_state_index;
        //private State current_state { get { return this.states[current_state_index]; } }

		private bool recovering;
		private int tokensSinceLastError;

		private readonly Stack<int> state_stack = new Stack<int>();
		protected ParserStack<ValueType, PositionType> value_stack = new ParserStack<ValueType, PositionType>();

		protected abstract string[] NonTerminals { get; }
		protected abstract State[] States { get; }
		protected abstract Rule[] Rules { get; }
		protected abstract int ErrorToken { get; }
		protected abstract int EofToken { get; }
        
		protected virtual PositionType InvalidPosition { get { return default(PositionType); } } 
		protected abstract PositionType CombinePositions(PositionType first, PositionType last);

		private readonly string[] nonTerminals;
		private readonly State[] states;
		private readonly Rule[] rules;
		private readonly int errToken;
		private readonly int eofToken;
		private readonly PositionType invalidPosition;

		protected ShiftReduceParser()
		{
			this.nonTerminals = NonTerminals;
			this.states = States;
			this.rules = Rules;
			this.errToken = ErrorToken;
			this.eofToken = EofToken;
			this.invalidPosition = InvalidPosition;

			if (states == null || rules == null || nonTerminals == null)
				throw new InvalidOperationException();
		}

		public bool Parse()
		{
			next = 0;
            current_state_index = 0;// current_state = states[0];

            state_stack.Push(current_state_index);
			value_stack.Push(yyval, yypos, yypos_valid);

            for (; ; )
            {
                //if (Trace)
                //    Console.Error.WriteLine("Entering state {0} ", states[current_state_index].num);

                int action = states[current_state_index].defaultAction;

                var current_state_parser_table = states[current_state_index].parser_table;
                if (current_state_parser_table != null)
                {
                    if (next == 0)
                    {
                        //if (Trace)
                        //    Console.Error.Write("Reading a token: ");

                        next = scanner.GetNextToken();
                    }

                    //if (Trace)
                    //    Console.Error.WriteLine("Next token is {0}", TerminalToString(next));

                    current_state_parser_table.TryGetValue(next, out action);
                }

                if (action > 0)         // shift
                {
                    Shift(action);
                }
                else if (action < 0)   // reduce
                {
                    Reduce(-action);

                    if (action == -1)	// accept
                        return true;
                }
                else if (action == 0)   // error
                {
                    if (!ErrorRecovery())
                        return false;
                }
            }
		}


		protected void Shift(int state_nr)
		{
            //if (Trace)
            //    Console.Error.Write("Shifting token {0}, ", TerminalToString(next));

            current_state_index = state_nr; //current_state = states[state_nr];

			value_stack.Push(scanner.TokenValue, scanner.TokenPosition, true);
			state_stack.Push(current_state_index);

			if (recovering)
			{
				if (next != errToken)
					tokensSinceLastError++;

				if (tokensSinceLastError > 5)
					recovering = false;
			}

			if (next != eofToken)
				next = 0;
		}

		private int currentRule;

		protected void Reduce(int rule_nr)
		{
            //if (Trace)
            //    DisplayRule(rule_nr);

			Rule rule = rules[rule_nr];
            var/*!*/rule_rhs = rule.rhs;

            if (rule_rhs.Length == 1)                   // LHS : RHS{0};
			{
				// $$ = $1;
				// @$ = @1;
				yyval = value_stack.PeekValue();
				yypos = value_stack.PeekPosition();
				yypos_valid = value_stack.IsValidPosition();
			}
            else if (rule_rhs.Length > 1)               // LHS : RHS{n - 1} RHS{n - 2} ... RHS{1} RHS{0} 
			{
				// $$ = { result of the semantic action };
				// @$ = @1 + @n;

                int first = rule_rhs.Length - 1;
				while (first >= 0 && !value_stack.IsValidPosition(first)) first--;

				if (first > 0)
				{
					int last = 0;
					while (!value_stack.IsValidPosition(last)) last++;

					yypos = CombinePositions(value_stack.PeekPosition(first), value_stack.PeekPosition(last));
					yypos_valid = true;
				}
				else
				{
					yypos = invalidPosition;
					yypos_valid = false;
				}
				
				yyval = default(ValueType);
			}
            else if (rule_rhs.Length == 0)              // LHS : ; 
			{
				// $$ = null;
				// @$ = <invalid>  -- empty reductions has no position
				
				// alternatives:
				// 1) @$ = scanner.TokenPosition -- the position of the following token, which triggered the reduction
				// 2) keep @$ unchanged -- the position of the last reduction/token (shift token would set yypos to skip tokens)
				// problems: both alternatives give bad results as they make reductions with combined position wider than they are
				
				yyval = default(ValueType);
				yypos = invalidPosition;
				yypos_valid = false;
			}
			
            //if (Trace)
            //    Console.Error.WriteLine("Rule position: {0}", yypos);

			currentRule = rule_nr;
            DoAction(rule_nr);
			currentRule = -1;

            for (int i = rule_rhs.Length; i > 0; i--)
            {
                state_stack.Pop();
                value_stack.Pop();
            }

            //if (Trace)
            //    DisplayStack();

			current_state_index = state_stack.Peek();

            int goto_state;
            if (states[current_state_index].Goto.TryGetValue(rule.lhs, out goto_state))
                current_state_index = goto_state;

			state_stack.Push(current_state_index);
			value_stack.Push(yyval, yypos, yypos_valid);
		}


		protected abstract void DoAction(int action_nr);
		
		protected PositionType GetLeftValidPosition(int symbolIndex)
		{
			int index = rules[currentRule].rhs.Length - symbolIndex;
			while (!value_stack.IsValidPosition(index)) index++;
			
            //if (Trace)
            //    Console.Error.WriteLine("LeftValidPosition({0}) = {1}", symbolIndex, value_stack.PeekPosition(index));
				
			return value_stack.PeekPosition(index);
		}


		protected bool ErrorRecovery()
		{
			if (!recovering) // if not recovering from previous error
				ReportError();

			recovering = true;
			tokensSinceLastError = 0;

			if (!FindErrorRecoveryState())
				return false;

			ShiftErrorToken();

			return DiscardInvalidTokens();
		}


		internal void ReportError()
		{
			string[] expected_terminals = null;

            var current_state_parser_table = states[current_state_index].parser_table;
            if (current_state_parser_table != null)
			{
                expected_terminals = new string[current_state_parser_table.Count];

				int i = 0;
                foreach (int terminal in current_state_parser_table.Keys)
					expected_terminals[i++] = TerminalToString(terminal);
			}

			scanner.ReportError(expected_terminals);
		}


		internal void ShiftErrorToken()
		{
			int old_next = next;
			next = errToken;

			Shift(states[current_state_index].parser_table[next]);

            //if (Trace)
            //    Console.Error.WriteLine("Entering state {0} ", states[current_state_index].num);

			next = old_next;
		}


		internal bool FindErrorRecoveryState()
		{
			while (true)    // pop states until one found that accepts error token
			{
                int i;
                var current_state_parser_table = states[current_state_index].parser_table;
                if (current_state_parser_table != null &&
                    current_state_parser_table.TryGetValue(errToken, out i) && //current_state_parser_table.ContainsKey(errToken) &&
                    i /*current_state_parser_table[errToken]*/ > 0) // shift
					return true;

                //if (Trace)
                //    Console.Error.WriteLine("Error: popping state {0}", states[state_stack.Peek()].num);

				state_stack.Pop();
				value_stack.Pop();

                //if (Trace)
                //    DisplayStack();

				if (state_stack.Count == 0)
				{
                    //if (Trace)
                    //    Console.Error.Write("Aborting: didn't find a state that accepts error token");
					return false;
				}
				else
					current_state_index = state_stack.Peek();
			}
		}


		internal bool DiscardInvalidTokens()
		{

			int action = states[current_state_index].defaultAction;

            var current_state_parser_table = states[current_state_index].parser_table;
            if (current_state_parser_table != null)
			{
				// Discard tokens until find one that works ...
				while (true)
				{
					if (next == 0)
					{
                        //if (Trace)
                        //    Console.Error.Write("Reading a token: ");

						next = scanner.GetNextToken();
					}

                    //if (Trace)
                    //    Console.Error.WriteLine("Next token is {0}", TerminalToString(next));

					if (next == eofToken)
						return false;

                    int i;
                    if (current_state_parser_table.TryGetValue(next, out i))
                        action = i;// current_state.parser_table[next];

					if (action != 0)
						return true;
					else
					{
                        //if (Trace)
                        //    Console.Error.WriteLine("Error: Discarding {0}", TerminalToString(next));
						next = 0;
					}
				}
			}
			else
				return true;
		}


		protected void yyerrok()
		{
			recovering = false;
		}

        //protected void AddState(int statenr, State state)
        //{
        //    states[statenr] = state;
        //    state.num = statenr;
        //}

        //private void DisplayStack()
        //{
        //    Console.Error.Write("State now");
        //    foreach (var state in state_stack)
        //        Console.Error.Write(" {0}", states[state].num);
        //    Console.Error.WriteLine();
        //}


        //private void DisplayRule(int rule_nr)
        //{
        //    Console.Error.Write("Reducing stack by rule {0}, ", rule_nr);
        //    DisplayProduction(rules[rule_nr]);
        //}


        //private void DisplayProduction(Rule rule)
        //{
        //    if (rule.rhs.Length == 0)
        //        Console.Error.Write("/* empty */ ");
        //    else
        //        foreach (int symbol in rule.rhs)
        //            Console.Error.Write("{0} ", SymbolToString(symbol));

        //    Console.Error.WriteLine("-> {0}", SymbolToString(rule.lhs));
        //}


		protected abstract string TerminalToString(int terminal);


        //private string SymbolToString(int symbol)
        //{
        //    if (symbol < 0)
        //        return nonTerminals[-symbol];
        //    else
        //        return TerminalToString(symbol);
        //}


		protected string CharToString(char ch)
		{
			switch (ch)
			{
				case '\a': return @"'\a'";
				case '\b': return @"'\b'";
				case '\f': return @"'\f'";
				case '\n': return @"'\n'";
				case '\r': return @"'\r'";
				case '\t': return @"'\t'";
				case '\v': return @"'\v'";
				case '\0': return @"'\0'";
				default: return string.Format("'{0}'", ch);
			}
		}
	}

	#endregion
}
