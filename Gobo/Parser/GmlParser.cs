﻿using Gobo.SyntaxNodes;
using Gobo.SyntaxNodes.Gml;
using Gobo.SyntaxNodes.Gml.Literals;

namespace Gobo.Parser;

internal struct GmlParseResult
{
    public GmlSyntaxNode Ast;
    public List<GmlSyntaxError> Errors;
    public List<CommentGroup> CommentGroups;
}

internal class GmlSyntaxErrorException : Exception
{
    public GmlSyntaxErrorException(string message)
        : base(message) { }
}

internal readonly struct GmlSyntaxError
{
    public string Message { get; init; }

    public GmlSyntaxError(string message)
    {
        Message = message;
    }
}

internal class GmlParser
{
    public Token CurrentToken => token;

    public List<CommentGroup> CommentGroups { get; private set; } = new();

    public int LineNumber { get; private set; } = 1;
    public int ColumnNumber { get; private set; } = 1;
    public List<GmlSyntaxError> Errors { get; private set; } = new();
    public bool Strict { get; set; } = true;

    private Token token;
    private Token accepted;
    private readonly GmlLexer lexer;
    private List<Token> currentCommentGroup = new();
    private bool HitEOF => token.Kind == TokenKind.Eof;

    private delegate bool BinaryExpressionRule(out GmlSyntaxNode node);

    public GmlParser(string code)
    {
        lexer = new GmlLexer(new StringReader(code));
        token = lexer.NextToken();
        ProcessToken(token);
    }

    public GmlParser(TextReader reader)
    {
        lexer = new GmlLexer(reader);
        token = lexer.NextToken();
        ProcessToken(token);
    }

    public GmlParseResult Parse()
    {
        Document(out var ast);
        return new GmlParseResult()
        {
            Ast = ast,
            CommentGroups = CommentGroups,
            Errors = Errors
        };
    }

    private void SetLexerMode(GmlLexer.LexerMode mode)
    {
        lexer.Mode = mode;
    }

    private void NextToken()
    {
        token = lexer.NextToken();
        LineNumber = token.Line;
        ColumnNumber = token.Column;
        ProcessToken(token);
    }

    private bool Accept(TokenKind kind, bool skipWhitespace = true)
    {
        if (skipWhitespace)
        {
            while (!HitEOF && IsHiddenToken(token))
            {
                NextToken();
            }
        }

        if (token.Kind == kind)
        {
            accepted = token;
            NextToken();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Same as Accept, but does not call NextToken
    /// </summary>
    private bool Consume(TokenKind kind, bool skipWhitespace = true)
    {
        if (skipWhitespace)
        {
            while (!HitEOF && IsHiddenToken(token))
            {
                NextToken();
            }
        }

        if (token.Kind == kind)
        {
            accepted = token;
            return true;
        }

        return false;
    }

    private void Expect(TokenKind kind, bool skipWhitespace = true)
    {
        if (!Accept(kind, skipWhitespace))
        {
            AddDefaultSyntaxError();
        }
    }

    private void Expect(bool returnValue)
    {
        if (!returnValue)
        {
            AddDefaultSyntaxError();
        }
    }

    private bool AcceptAny(params TokenKind[] types)
    {
        foreach (var type in types)
        {
            if (Accept(type))
            {
                return true;
            }
        }
        return false;
    }

    private static TextSpan GetSpan(Token firstToken, Token lastToken)
    {
        return new TextSpan(firstToken.StartIndex, lastToken.EndIndex);
    }

    private static TextSpan GetSpan(Token token)
    {
        return new TextSpan(token.StartIndex, token.EndIndex);
    }

    private static bool IsHiddenToken(Token tok)
    {
        return tok.Kind == TokenKind.Whitespace
            || tok.Kind == TokenKind.LineBreak
            || tok.Kind == TokenKind.SingleLineComment
            || tok.Kind == TokenKind.MultiLineComment;
    }

    private void ProcessToken(Token tok)
    {
        switch (tok.Kind)
        {
            case TokenKind.Whitespace:
                if (currentCommentGroup.Count > 0)
                {
                    currentCommentGroup.Add(tok);
                }
                break;
            case TokenKind.LineBreak:
            case TokenKind.Eof:
                AcceptCommentGroup();
                break;
            case TokenKind.SingleLineComment:
            case TokenKind.MultiLineComment:
                currentCommentGroup.Add(tok);
                break;
        }
    }

    private void AcceptCommentGroup()
    {
        if (currentCommentGroup.Count == 0)
        {
            return;
        }

        if (currentCommentGroup.Count > 1)
        {
            var group = currentCommentGroup.AsEnumerable();
            while (
                group.Last().Kind == TokenKind.Whitespace
                || group.Last().Kind == TokenKind.LineBreak
            )
            {
                group = group.SkipLast(1);
            }
            currentCommentGroup = group.ToList();
        }

        CommentGroups.Add(
            new CommentGroup(
                new(currentCommentGroup),
                new TextSpan(
                    currentCommentGroup.First().StartIndex,
                    currentCommentGroup.Last().EndIndex
                )
            )
        );

        currentCommentGroup.Clear();
    }

    private void AddError(string message)
    {
        var positionMessage = $"Syntax error at line {token.Line}, column {token.Column}:\n";
        Errors.Add(new GmlSyntaxError(positionMessage + message));
        throw new GmlSyntaxErrorException(Errors.Last().Message);
    }

    private void ThrowUnexpected(Token token)
    {
        var symbolText = token.Text == "<EOF>" ? "end of file" : $"'{token.Text}'";
        var offendingSymbolMessage = $"unexpected {symbolText} [{token.Kind.ToString()}]";
        AddError(offendingSymbolMessage);
    }

    private void ThrowExpected(string symbol)
    {
        var offendingSymbolMessage = $"expected '{symbol}'";
        AddError(offendingSymbolMessage);
    }

    private void AddDefaultSyntaxError()
    {
        ThrowUnexpected(CurrentToken);
    }

    private void Document(out GmlSyntaxNode node)
    {
        var start = token;

        var statements = AcceptStatementList();

        node = new Document(GetSpan(start, accepted), statements);
    }

    private List<GmlSyntaxNode> AcceptStatementList()
    {
        var statements = new List<GmlSyntaxNode>();

        while (!HitEOF)
        {
            if (Accept(TokenKind.SemiColon))
            {
                continue;
            }

            if (Statement(out var result, acceptSemicolons: false))
            {
                statements.Add(result);
            }
            else
            {
                break;
            }
        }

        return statements;
    }

    #region Statements

    private bool Statement(out GmlSyntaxNode result, bool acceptSemicolons = true)
    {
        if (
            Block(out result)
            || AssignmentOrExpressionStatement(out result)
            || IfStatement(out result)
            || FunctionDeclaration(out result)
            || DoStatement(out result)
            || WhileStatement(out result)
            || WithStatement(out result)
            || RepeatStatement(out result)
            || ForStatement(out result)
            || ThrowStatement(out result)
            || ContinueStatement(out result)
            || BreakStatement(out result)
            || ExitStatement(out result)
            || ReturnStatement(out result)
            || RegionStatement(out result)
            || SwitchStatement(out result)
            || TryStatement(out result)
            || DefineStatement(out result)
            || DeleteStatement(out result)
            || EnumeratorDeclaration(out result)
            || MacroStatement(out result)
        )
        {
            if (acceptSemicolons)
            {
                while (!HitEOF)
                {
                    if (!Accept(TokenKind.SemiColon))
                    {
                        break;
                    }
                }
            }

            return true;
        }
        else
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }
    }

    private bool Block(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.OpenBrace))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        var statements = AcceptStatementList();

        Expect(TokenKind.CloseBrace);

        result = new Block(GetSpan(start, accepted), statements);
        return true;
    }

    private bool FunctionDeclaration(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.Function))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        Identifier(out var identifier);
        Expect(ParameterList(out var parameters));

        GmlSyntaxNode constructorClause = GmlSyntaxNode.Empty;

        if (Accept(TokenKind.Colon))
        {
            GmlSyntaxNode parentName = GmlSyntaxNode.Empty;
            GmlSyntaxNode parentArgs = GmlSyntaxNode.Empty;
            // Parent name cannot be 'constructor'
            Expect(TokenKind.Identifier);
            parentName = new Identifier(GetSpan(accepted), accepted.Text);

            Expect(ArgumentList(out parentArgs));
            Expect(TokenKind.Constructor);

            constructorClause = new ConstructorClause(
                GetSpan(start, accepted),
                parentName,
                parentArgs
            );
        }
        else if (Accept(TokenKind.Constructor))
        {
            constructorClause = new ConstructorClause(
                GetSpan(start, accepted),
                GmlSyntaxNode.Empty,
                GmlSyntaxNode.Empty
            );
        }

        Expect(Statement(out var body));

        result = new FunctionDeclaration(
            GetSpan(start, accepted),
            identifier,
            parameters,
            body,
            constructorClause
        );
        return true;
    }

    private bool AssignmentOrExpressionStatement(out GmlSyntaxNode result)
    {
        var start = token;
        result = GmlSyntaxNode.Empty;

        if (AcceptAny(TokenKind.Var, TokenKind.Static, TokenKind.GlobalVar))
        {
            // Variable declaration list
            var modifier = accepted.Text;

            while (Accept(TokenKind.Var))
            {
                continue;
            }

            if (!VariableDeclarator(out var firstDeclaration))
            {
                AddDefaultSyntaxError();
                return false;
            }

            var declarations = new List<GmlSyntaxNode>() { firstDeclaration };

            while (!HitEOF)
            {
                if (Accept(TokenKind.Comma))
                {
                    Expect(VariableDeclarator(out var variableDeclarator));
                    declarations.Add(variableDeclarator);
                }
                else
                {
                    break;
                }
            }

            result = new VariableDeclarationList(GetSpan(start, accepted), declarations, modifier);
        }
        else if (UnaryExpression(out var left))
        {
            // expression statement
            if (left is UnaryExpression { Operator: "++" or "--" } or CallExpression)
            {
                result = left;
                return true;
            }

            // assignment
            if (
                !AcceptAny(
                    TokenKind.Assign,
                    TokenKind.MultiplyAssign,
                    TokenKind.DivideAssign,
                    TokenKind.PlusAssign,
                    TokenKind.MinusAssign,
                    TokenKind.ModulusAssign,
                    TokenKind.LeftShiftArithmeticAssign,
                    TokenKind.RightShiftArithmeticAssign,
                    TokenKind.BitAndAssign,
                    TokenKind.BitXorAssign,
                    TokenKind.BitOrAssign
                )
            )
            {
                if (Strict)
                {
                    AddError($"unexpected expression");
                }
            }

            var assignmentOperator = accepted.Text;

            Expect(Expression(out var right));

            result = new AssignmentExpression(
                GetSpan(start, accepted),
                assignmentOperator,
                left,
                right,
                GmlSyntaxNode.Empty
            );
        }
        else
        {
            return false;
        }

        return true;
    }

    private bool IfStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.If))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        Expect(Expression(out var condition));
        Accept(TokenKind.Then);
        Expect(Statement(out var statement));

        GmlSyntaxNode alternate = GmlSyntaxNode.Empty;
        if (Accept(TokenKind.Else))
        {
            Expect(Statement(out alternate));
        }

        result = new IfStatement(GetSpan(start, accepted), condition, statement, alternate);
        return true;
    }

    private bool DoStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.Do))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        Expect(Statement(out var body));
        Expect(TokenKind.Until);
        Expect(Expression(out var test));

        result = new DoStatement(GetSpan(start, accepted), body, test);
        return true;
    }

    private bool WhileStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.While))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        Expect(Expression(out var test));
        Expect(Statement(out var body));

        result = new WhileStatement(GetSpan(start, accepted), test, body);
        return true;
    }

    private bool RepeatStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.Repeat))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        Expect(Expression(out var test));
        Expect(Statement(out var body));

        result = new RepeatStatement(GetSpan(start, accepted), test, body);
        return true;
    }

    private bool WithStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.With))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        Expect(Expression(out var @object));
        Expect(Statement(out var body));

        result = new WithStatement(GetSpan(start, accepted), @object, body);
        return true;
    }

    private bool ForStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.For))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        Expect(TokenKind.OpenParen);
        Statement(out var init, acceptSemicolons: false);
        Expect(TokenKind.SemiColon);
        Expression(out var test);
        Expect(TokenKind.SemiColon);
        Statement(out var update, acceptSemicolons: true);
        Expect(TokenKind.CloseParen);
        Expect(Statement(out var body));

        result = new ForStatement(GetSpan(start, accepted), init, test, update, body);
        return true;
    }

    private bool SwitchStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.Switch))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        Expect(Expression(out var condition));

        var blockStart = token;
        Expect(TokenKind.OpenBrace);

        var cases = new List<GmlSyntaxNode>();
        while (!HitEOF)
        {
            if (SwitchCase(out var switchCase))
            {
                cases.Add(switchCase);
            }
            else
            {
                break;
            }
        }

        Expect(TokenKind.CloseBrace);

        var caseBlock = new SwitchBlock(GetSpan(blockStart, token), cases);
        result = new SwitchStatement(GetSpan(start, accepted), condition, caseBlock);
        return true;
    }

    private bool SwitchCase(out GmlSyntaxNode result)
    {
        var start = token;
        if (Accept(TokenKind.Case))
        {
            Expect(Expression(out var test));
            Expect(TokenKind.Colon);
            var statements = AcceptStatementList();
            result = new SwitchCase(GetSpan(start, accepted), test, statements);
        }
        else if (Accept(TokenKind.Default))
        {
            Expect(TokenKind.Colon);
            var statements = AcceptStatementList();
            result = new SwitchCase(GetSpan(start, accepted), GmlSyntaxNode.Empty, statements);
        }
        else
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        return true;
    }

    private bool ContinueStatement(out GmlSyntaxNode result)
    {
        if (!Accept(TokenKind.Continue))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }
        result = new ContinueStatement(GetSpan(accepted));
        return true;
    }

    private bool BreakStatement(out GmlSyntaxNode result)
    {
        if (!Accept(TokenKind.Break))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }
        result = new BreakStatement(GetSpan(accepted));
        return true;
    }

    private bool ExitStatement(out GmlSyntaxNode result)
    {
        if (!Accept(TokenKind.Exit))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }
        result = new ExitStatement(GetSpan(accepted));
        return true;
    }

    private bool ReturnStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.Return))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }
        Expression(out var argument);
        result = new ReturnStatement(GetSpan(start, accepted), argument);
        return true;
    }

    private bool ThrowStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.Throw))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }
        Expression(out var argument);
        result = new ThrowStatement(GetSpan(start, accepted), argument);
        return true;
    }

    private bool DeleteStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.Delete))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }
        Expression(out var argument);
        result = new DeleteStatement(GetSpan(start, accepted), argument);
        return true;
    }

    private bool RegionStatement(out GmlSyntaxNode result)
    {
        var start = token;
        result = GmlSyntaxNode.Empty;
        if (!AcceptAny(TokenKind.Region, TokenKind.EndRegion))
        {
            return false;
        }

        bool isEndRegion = accepted.Kind == TokenKind.EndRegion;

        string name = string.Empty;
        if (Accept(TokenKind.RegionName))
        {
            name = accepted.Text;
        }

        result = new RegionStatement(GetSpan(start, accepted), name, isEndRegion);
        return true;
    }

    private bool DefineStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.Define))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        string name = string.Empty;
        if (Accept(TokenKind.RegionName))
        {
            name = accepted.Text;
        }

        result = new DefineStatement(new TextSpan(start.StartIndex, accepted.EndIndex), name);
        return true;
    }

    private bool MacroStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.Macro))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        Expect(TokenKind.Identifier);
        var name = accepted.Text;

        var tokens = new List<string>();
        bool ignoreNextLineBreak = false;

        Accept(TokenKind.Whitespace);

        while (!HitEOF)
        {
            string tokenText;

            if (Accept(TokenKind.LineBreak, skipWhitespace: false))
            {
                if (ignoreNextLineBreak)
                {
                    tokenText = accepted.Text;
                    ignoreNextLineBreak = false;
                }
                else
                {
                    break;
                }
            }
            else if (Accept(TokenKind.Backslash, skipWhitespace: false))
            {
                tokenText = @"\";
                ignoreNextLineBreak = true;
            }
            else if (Accept(TokenKind.Whitespace, skipWhitespace: false))
            {
                tokenText = accepted.Text;
            }
            else
            {
                tokenText = token.Text;
                ignoreNextLineBreak = false;
                NextToken();
            }

            tokens.Add(tokenText);

            if (token.Kind == TokenKind.Eof)
            {
                break;
            }
        }

        result = new MacroDeclaration(
            GetSpan(start, accepted),
            name,
            string.Join("", tokens).TrimEnd()
        );
        return true;
    }

    private bool TryStatement(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.Try))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        GmlSyntaxNode catchProduction = GmlSyntaxNode.Empty;
        GmlSyntaxNode finallyProduction = GmlSyntaxNode.Empty;

        Expect(Statement(out var tryBody));

        if (Accept(TokenKind.Catch))
        {
            GmlSyntaxNode identifier = GmlSyntaxNode.Empty;
            if (Accept(TokenKind.OpenParen))
            {
                Expect(Identifier(out identifier));
                Expect(TokenKind.CloseParen);
            }
            Expect(Statement(out var body));
            catchProduction = new CatchProduction(GetSpan(start, accepted), identifier, body);
        }

        if (Accept(TokenKind.Finally))
        {
            Expect(Statement(out var finallyBody));
            finallyProduction = new FinallyProduction(GetSpan(start, accepted), finallyBody);
        }

        result = new TryStatement(
            GetSpan(start, accepted),
            tryBody,
            catchProduction,
            finallyProduction
        );
        return true;
    }

    private bool EnumeratorDeclaration(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.Enum))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        Expect(Identifier(out var name));

        var startBlock = token;
        Expect(TokenKind.OpenBrace);

        if (Accept(TokenKind.CloseBrace))
        {
            result = new EnumDeclaration(
                GetSpan(start, accepted),
                name,
                new EnumBlock(GetSpan(startBlock, token), new())
            );
            return true;
        }

        var enumMembers = new List<GmlSyntaxNode>();
        bool expectDelimiter = false;

        while (!HitEOF)
        {
            if (expectDelimiter)
            {
                Expect(TokenKind.Comma);
            }
            else
            {
                Expect(EnumMember(out var enumMember));
                enumMembers.Add(enumMember);
            }
            expectDelimiter = !expectDelimiter;

            if (Accept(TokenKind.CloseBrace))
            {
                break;
            }
        }

        if (accepted.Kind != TokenKind.CloseBrace)
        {
            ThrowExpected("}");
        }

        var enumBlock = new EnumBlock(GetSpan(startBlock, token), enumMembers);
        result = new EnumDeclaration(GetSpan(start, accepted), name, enumBlock);
        return true;
    }

    private bool EnumMember(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Identifier(out var identifier))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        GmlSyntaxNode expression = GmlSyntaxNode.Empty;
        if (Accept(TokenKind.Assign))
        {
            Expect(Expression(out expression));
        }

        result = new EnumMember(GetSpan(start, accepted), identifier, expression);
        return true;
    }

    private bool VariableDeclarator(out GmlSyntaxNode result)
    {
        var start = token;

        if (!Identifier(out var identifier))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }
        GmlSyntaxNode expression = GmlSyntaxNode.Empty;
        if (Accept(TokenKind.Assign))
        {
            Expect(Expression(out expression));
        }

        result = new VariableDeclarator(
            GetSpan(start, accepted),
            identifier,
            GmlSyntaxNode.Empty,
            expression
        );
        return true;
    }

    #endregion

    #region Expressions
    private bool PrimaryExpression(out GmlSyntaxNode result)
    {
        var start = token;

        if (!PrimaryExpressionStart(out var startExpression))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        GmlSyntaxNode @object = startExpression;

        while (!HitEOF)
        {
            if (
                Accept(TokenKind.PlusPlus, skipWhitespace: false)
                || Accept(TokenKind.MinusMinus, skipWhitespace: false)
            )
            {
                var @operator = accepted.Text;
                @object = new UnaryExpression(
                    GetSpan(start, accepted),
                    @operator,
                    @object,
                    isPrefix: false
                );
                break;
            }
            else if (
                AcceptAny(
                    TokenKind.OpenBracket,
                    TokenKind.ArrayAccessor,
                    TokenKind.ListAccessor,
                    TokenKind.MapAccessor,
                    TokenKind.GridAccessor,
                    TokenKind.ArrayAccessor,
                    TokenKind.StructAccessor
                )
            )
            {
                var accessor = accepted.Text;
                var expressions = new List<GmlSyntaxNode>();
                Expect(Expression(out var firstExpression));
                expressions.Add(firstExpression);

                while (!HitEOF)
                {
                    if (Accept(TokenKind.Comma))
                    {
                        Expect(Expression(out var expression));
                        expressions.Add(expression);
                    }
                    else if (Accept(TokenKind.CloseBracket))
                    {
                        break;
                    }
                    else
                    {
                        AddDefaultSyntaxError();
                    }
                }

                @object = new MemberIndexExpression(
                    GetSpan(start, accepted),
                    @object,
                    expressions,
                    accessor
                );
            }
            else if (Accept(TokenKind.Dot))
            {
                Expect(Identifier(out var identifier));
                @object = new MemberDotExpression(GetSpan(start, accepted), @object, identifier);
            }
            else if (ArgumentList(out var arguments))
            {
                @object = new CallExpression(GetSpan(start, accepted), @object, arguments);
            }
            else
            {
                break;
            }
        }

        result = @object;
        return true;
    }

    private bool PrimaryExpressionStart(out GmlSyntaxNode result)
    {
        var start = token;

        if (Literal(out var literal))
        {
            result = literal;
        }
        else if (Identifier(out var identifier))
        {
            result = identifier;
        }
        else if (Accept(TokenKind.New))
        {
            GmlSyntaxNode id = GmlSyntaxNode.Empty;
            if (Identifier(out var constructorName))
            {
                id = constructorName;
            }
            Expect(ArgumentList(out var arguments));
            result = new NewExpression(GetSpan(start, accepted), id, arguments);
        }
        else if (Accept(TokenKind.OpenParen))
        {
            Expect(Expression(out var expression));
            Expect(TokenKind.CloseParen);
            result = new ParenthesizedExpression(GetSpan(start, accepted), expression);
        }
        else
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        return true;
    }

    private bool Expression(out GmlSyntaxNode result)
    {
        if (FunctionDeclaration(out result))
        {
            return true;
        }
        return ConditionalExpression(out result);
    }

    private bool ConditionalExpression(out GmlSyntaxNode result)
    {
        var start = token;

        if (!(BitXorExpression(out result)))
        {
            return false;
        }

        if (Accept(TokenKind.QuestionMark))
        {
            Expect(Expression(out var whenTrue));
            Expect(TokenKind.Colon);
            Expect(Expression(out var whenFalse));

            result = new ConditionalExpression(
                GetSpan(start, accepted),
                result,
                whenTrue,
                whenFalse
            );
        }

        return true;
    }

    // TODO: optimize if needed
    private bool HandleBinaryExpression(
        BinaryExpressionRule nextRule,
        out GmlSyntaxNode result,
        params TokenKind[] tokenTypes
    )
    {
        var start = token;

        if (!nextRule(out result))
        {
            return false;
        }

        while (AcceptAny(tokenTypes))
        {
            var @operator = accepted.Text;
            Expect(nextRule(out var right));
            result = new BinaryExpression(GetSpan(start, accepted), @operator, result, right);
        }

        return true;
    }

    private bool BitXorExpression(out GmlSyntaxNode result)
    {
        return HandleBinaryExpression(BitOrExpression, out result, TokenKind.BitXor);
    }

    private bool BitOrExpression(out GmlSyntaxNode result)
    {
        return HandleBinaryExpression(BitAndExpression, out result, TokenKind.BitOr);
    }

    private bool BitAndExpression(out GmlSyntaxNode result)
    {
        return HandleBinaryExpression(NullCoalescingExpression, out result, TokenKind.BitAnd);
    }

    private bool NullCoalescingExpression(out GmlSyntaxNode result)
    {
        return HandleBinaryExpression(XorExpression, out result, TokenKind.NullCoalesce);
    }

    private bool XorExpression(out GmlSyntaxNode result)
    {
        return HandleBinaryExpression(AndExpression, out result, TokenKind.Xor);
    }

    private bool AndExpression(out GmlSyntaxNode result)
    {
        return HandleBinaryExpression(OrExpression, out result, TokenKind.And);
    }

    private bool OrExpression(out GmlSyntaxNode result)
    {
        return HandleBinaryExpression(EqualityExpression, out result, TokenKind.Or);
    }

    private bool EqualityExpression(out GmlSyntaxNode result)
    {
        return HandleBinaryExpression(
            RelationalExpression,
            out result,
            TokenKind.Equals,
            TokenKind.Assign,
            TokenKind.NotEquals
        );
    }

    private bool RelationalExpression(out GmlSyntaxNode result)
    {
        return HandleBinaryExpression(
            ShiftExpression,
            out result,
            TokenKind.LessThan,
            TokenKind.GreaterThan,
            TokenKind.LessThanEquals,
            TokenKind.GreaterThanEquals
        );
    }

    private bool ShiftExpression(out GmlSyntaxNode result)
    {
        return HandleBinaryExpression(
            AdditiveExpression,
            out result,
            TokenKind.LeftShiftArithmetic,
            TokenKind.RightShiftArithmetic
        );
    }

    private bool AdditiveExpression(out GmlSyntaxNode result)
    {
        return HandleBinaryExpression(
            MultiplicativeExpression,
            out result,
            TokenKind.Plus,
            TokenKind.Minus
        );
    }

    private bool MultiplicativeExpression(out GmlSyntaxNode result)
    {
        return HandleBinaryExpression(
            UnaryExpression,
            out result,
            TokenKind.Multiply,
            TokenKind.Divide,
            TokenKind.Modulo,
            TokenKind.IntegerDivide
        );
    }

    private bool UnaryExpression(out GmlSyntaxNode result)
    {
        var start = token;

        if (
            AcceptAny(
                TokenKind.Plus,
                TokenKind.Minus,
                TokenKind.Not,
                TokenKind.BitNot,
                TokenKind.PlusPlus,
                TokenKind.MinusMinus
            )
        )
        {
            // Increment/decrement operators must be adjacent to their operands
            if (
                (accepted.Kind == TokenKind.PlusPlus || accepted.Kind == TokenKind.MinusMinus)
                && (token.Kind == TokenKind.Whitespace || token.Kind == TokenKind.LineBreak)
            )
            {
                ThrowUnexpected(accepted);
            }

            var @operator = accepted.Text;
            Expect(PrimaryExpression(out var primaryExpression));
            result = new UnaryExpression(
                GetSpan(start, accepted),
                @operator,
                primaryExpression,
                isPrefix: true
            );
            return true;
        }

        return PrimaryExpression(out result);
    }

    #endregion

    private bool ParameterList(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.OpenParen))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        if (Accept(TokenKind.CloseParen))
        {
            result = new ParameterList(GetSpan(start, accepted), new());
            return true;
        }

        Expect(Parameter(out var firstParameter));
        var parameters = new List<GmlSyntaxNode>() { firstParameter };

        while (!HitEOF)
        {
            if (Accept(TokenKind.Comma))
            {
                Expect(Parameter(out var parameter));
                parameters.Add(parameter);
            }
            else if (Accept(TokenKind.CloseParen))
            {
                break;
            }
            else
            {
                AddDefaultSyntaxError();
                result = GmlSyntaxNode.Empty;
                return false;
            }
        }

        result = new ParameterList(GetSpan(start, accepted), parameters);
        return true;
    }

    private bool Parameter(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Identifier(out var identifier))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        GmlSyntaxNode initializer = GmlSyntaxNode.Empty;

        if (Accept(TokenKind.Assign))
        {
            Expect(Expression(out initializer));
        }

        result = new Parameter(
            GetSpan(start, accepted),
            identifier,
            GmlSyntaxNode.Empty,
            initializer
        );
        return true;
    }

    private bool ArgumentList(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.OpenParen))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        if (Accept(TokenKind.CloseParen))
        {
            result = new ArgumentList(GetSpan(start, accepted), new());
            return true;
        }

        var arguments = new List<GmlSyntaxNode>();
        bool previousChildWasPunctuator = true;

        while (!HitEOF)
        {
            if (Expression(out var expression))
            {
                if (!previousChildWasPunctuator)
                {
                    AddError("Expected ','");
                    result = GmlSyntaxNode.Empty;
                    return false;
                }
                previousChildWasPunctuator = false;
                arguments.Add(expression);
            }

            if (Accept(TokenKind.Comma))
            {
                if (previousChildWasPunctuator)
                {
                    arguments.Add(new UndefinedArgument(token.StartIndex - 1));
                }
                previousChildWasPunctuator = true;
            }
            else if (Accept(TokenKind.CloseParen))
            {
                if (previousChildWasPunctuator && arguments.Count > 0)
                {
                    arguments.Add(new UndefinedArgument(token.StartIndex - 1));
                }
                break;
            }
            else
            {
                AddDefaultSyntaxError();
                result = GmlSyntaxNode.Empty;
                return false;
            }
        }

        result = new ArgumentList(GetSpan(start, accepted), arguments);
        return true;
    }

    private bool Identifier(out GmlSyntaxNode result)
    {
        result = GmlSyntaxNode.Empty;

        if (Accept(TokenKind.Identifier))
        {
            result = new Identifier(GetSpan(accepted), accepted.Text);
            return true;
        }
        else if (Accept(TokenKind.Constructor))
        {
            result = new Identifier(GetSpan(accepted), accepted.Text);
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool Literal(out GmlSyntaxNode result)
    {
        var start = token;
        if (Accept(TokenKind.Undefined))
        {
            result = new UndefinedLiteral(GetSpan(accepted), accepted.Text);
        }
        else if (Accept(TokenKind.IntegerLiteral))
        {
            result = new IntegerLiteral(GetSpan(accepted), accepted.Text);
        }
        else if (Accept(TokenKind.DecimalLiteral))
        {
            result = new DecimalLiteral(GetSpan(accepted), accepted.Text);
        }
        else if (Accept(TokenKind.StringLiteral))
        {
            result = new StringLiteral(GetSpan(accepted), accepted.Text);
        }
        else if (Accept(TokenKind.SimpleTemplateString))
        {
            var fullSpan = GetSpan(accepted);
            var textSpan = new TextSpan(fullSpan.Start + 2, fullSpan.End - 1);

            var text = new TemplateText(textSpan, accepted.Text[2..^1]);
            result = new TemplateLiteral(GetSpan(accepted), new List<GmlSyntaxNode>() { text });
        }
        else if (Accept(TokenKind.TemplateStart))
        {
            // TODO: Simplify
            var fullSpan = GetSpan(accepted);
            var textSpan = new TextSpan(fullSpan.Start + 2, fullSpan.End - 1);
            var text = new TemplateText(textSpan, accepted.Text[2..^1]);
            var atoms = new List<GmlSyntaxNode>() { text };

            var expressionStart = accepted.EndIndex - 1;

            while (!HitEOF)
            {
                var expressionExists = Expression(out var expression);

                lexer.Mode = GmlLexer.LexerMode.TemplateString;

                Expect(TokenKind.CloseBrace);

                if (expressionExists)
                {
                    atoms.Add(
                        new TemplateExpression(
                            new TextSpan(expressionStart, accepted.EndIndex),
                            expression
                        )
                    );
                }

                lexer.Mode = GmlLexer.LexerMode.Default;

                if (Accept(TokenKind.TemplateMiddle))
                {
                    fullSpan = GetSpan(accepted);
                    textSpan = new TextSpan(fullSpan.Start, fullSpan.End - 1);
                    text = new TemplateText(textSpan, accepted.Text[0..^1]);
                    atoms.Add(text);
                    expressionStart = accepted.EndIndex;
                    continue;
                }
                else if (Accept(TokenKind.TemplateEnd))
                {
                    fullSpan = GetSpan(accepted);
                    textSpan = new TextSpan(fullSpan.Start, fullSpan.End - 1);
                    text = new TemplateText(textSpan, accepted.Text[0..^1]);
                    atoms.Add(text);

                    if (atoms[0] is TemplateText && atoms[0].Span.Length == 0)
                    {
                        atoms.RemoveAt(0);
                    }

                    if (atoms.Count > 0 && atoms[^1] is TemplateText && atoms[^1].Span.Length == 0)
                    {
                        atoms.RemoveAt(atoms.Count - 1);
                    }

                    result = new TemplateLiteral(
                        new TextSpan(start.StartIndex, accepted.EndIndex),
                        atoms
                    );
                    return true;
                }
                else
                {
                    ThrowUnexpected(token);
                }
            }

            AddDefaultSyntaxError();
            result = GmlSyntaxNode.Empty;
            return false;
        }
        else if (
            AcceptAny(
                TokenKind.Noone,
                TokenKind.BooleanLiteral,
                TokenKind.VerbatimStringLiteral,
                TokenKind.HexIntegerLiteral,
                TokenKind.BinaryLiteral
            )
        )
        {
            result = new Literal(GetSpan(accepted), accepted.Text);
        }
        else if (ArrayLiteral(out var arrayLiteral))
        {
            result = arrayLiteral;
        }
        else if (StructLiteral(out var structLiteral))
        {
            result = structLiteral;
        }
        else
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        return true;
    }

    private bool ArrayLiteral(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.OpenBracket))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        if (Accept(TokenKind.CloseBracket))
        {
            result = new ArrayExpression(GetSpan(start, accepted), new());
            return true;
        }

        var elements = new List<GmlSyntaxNode>();

        bool expectDelimiter = false;
        while (!HitEOF)
        {
            if (expectDelimiter)
            {
                Expect(TokenKind.Comma);
            }
            else
            {
                Expect(Expression(out var expression));
                elements.Add(expression);
            }
            expectDelimiter = !expectDelimiter;

            if (Accept(TokenKind.CloseBracket))
            {
                break;
            }
        }

        if (accepted.Kind != TokenKind.CloseBracket)
        {
            ThrowExpected("]");
        }

        result = new ArrayExpression(GetSpan(start, accepted), elements);
        return true;
    }

    private bool StructLiteral(out GmlSyntaxNode result)
    {
        var start = token;
        if (!Accept(TokenKind.OpenBrace))
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        if (Accept(TokenKind.CloseBrace))
        {
            result = new StructExpression(GetSpan(start, accepted), new());
            return true;
        }

        var properties = new List<GmlSyntaxNode>();

        bool expectDelimiter = false;
        while (!HitEOF)
        {
            if (expectDelimiter)
            {
                Expect(TokenKind.Comma);
            }
            else
            {
                Expect(PropertyAssignment(out var property));
                properties.Add(property);
            }
            expectDelimiter = !expectDelimiter;

            if (Accept(TokenKind.CloseBrace))
            {
                break;
            }
        }

        if (accepted.Kind != TokenKind.CloseBrace)
        {
            ThrowExpected("}");
        }

        result = new StructExpression(GetSpan(start, accepted), properties);
        return true;
    }

    private bool PropertyAssignment(out GmlSyntaxNode result)
    {
        var start = token;
        GmlSyntaxNode name = GmlSyntaxNode.Empty;

        if (AcceptAny(TokenKind.Identifier, TokenKind.Constructor, TokenKind.Noone))
        {
            name = new Identifier(GetSpan(accepted), accepted.Text);
        }
        else if (Accept(TokenKind.StringLiteral))
        {
            name = new StringLiteral(GetSpan(accepted), accepted.Text);
        }
        else
        {
            result = GmlSyntaxNode.Empty;
            return false;
        }

        Expect(TokenKind.Colon);
        Expect(Expression(out var initializer));

        result = new StructProperty(GetSpan(start, accepted), name, initializer);

        return true;
    }
}
