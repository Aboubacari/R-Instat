﻿' R- Instat
' Copyright (C) 2015-2017
'
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
'
' You should have received a copy of the GNU General Public License 
' along with this program.  If not, see <http://www.gnu.org/licenses/>.

Imports System.IO
Imports System.Windows.Controls
Imports ScintillaNET

Public Class ucrScript

    Private bIsTextChanged = False
    Private iMaxLineNumberCharLength As Integer = 0
    Private Const strComment As String = "Code run from Script Window"
    Private strRInstatLogFilesFolderPath As String = Path.Combine(Path.GetFullPath(FileIO.SpecialDirectories.MyDocuments), "R-Instat_Log_files")

    Friend WithEvents clsScriptActive As Scintilla

    ''' <summary>
    '''     The current text in the active tab. 
    ''' </summary>
    Public Property strText As String
        Get
            Return If(IsNothing(clsScriptActive), Nothing, clsScriptActive.Text)
        End Get
        Set(strNewText As String)
            If Not IsNothing(clsScriptActive) Then
                clsScriptActive.Text = strNewText
            End If
        End Set
    End Property

    ''' <summary>
    '''     Appends <paramref name="strText"/> to the end of the text in the active tab.    ''' 
    ''' </summary>
    ''' <param name="strText"> The text to append to the contents of the active tab.</param>
    Public Sub AppendText(strText As String)
        clsScriptActive.AppendText(Environment.NewLine & strText)
        clsScriptActive.GotoPosition(clsScriptActive.TextLength)
        EnableDisableButtons()
    End Sub

    ''' <summary>
    ''' Removes the selected text from the active tab, and copies the removed text to the clipboard.
    ''' </summary>
    Public Sub CutText()
        If clsScriptActive.SelectedText.Length > 0 Then
            clsScriptActive.Cut()
            EnableDisableButtons()
        End If
    End Sub

    ''' <summary>
    ''' Copies the selected text from the active tab to the clipboard.
    ''' </summary>
    Public Sub CopyText()
        If clsScriptActive.SelectedText.Length > 0 Then
            clsScriptActive.Copy()
            EnableDisableButtons()
        End If
    End Sub

    ''' <summary>
    ''' Pastes the contents of the clipboard into the active tab.
    ''' </summary>
    Public Sub PasteText()
        If Clipboard.ContainsData(DataFormats.Text) Then
            clsScriptActive.Paste()
            EnableDisableButtons()
        Else
            MsgBox("You can only paste text data on the script window.", MsgBoxStyle.Exclamation, "Paste to Script Window")
        End If
    End Sub

    ''' <summary>
    ''' Selects all the text in the active tab.
    ''' </summary>
    Public Sub SelectAllText()
        clsScriptActive.SelectAll()
        EnableDisableButtons()
    End Sub

    Private Sub addTab()
        clsScriptActive = NewScriptEditor()
        SetLineNumberMarginWidth(1, True)

        Dim tabPageAdded = New TabPage
        tabPageAdded.Controls.Add(clsScriptActive)
        tabPageAdded.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        tabPageAdded.ForeColor = System.Drawing.SystemColors.ControlText
        tabPageAdded.Location = New System.Drawing.Point(4, 22)
        tabPageAdded.Name = "TabPageAdded"
        tabPageAdded.Padding = New System.Windows.Forms.Padding(3)
        tabPageAdded.Size = New System.Drawing.Size(397, 415)
        tabPageAdded.TabIndex = 0
        tabPageAdded.UseVisualStyleBackColor = True

        TabControl.TabPages.Add(tabPageAdded)
        Static iTabCounter As Integer = 1
        tabPageAdded.Text = "Untitled" & iTabCounter
        iTabCounter += 1

        TabControl.SelectedTab = tabPageAdded
        bIsTextChanged = False
        EnableDisableButtons()
    End Sub

    Private Sub EnableDisableButtons()
        mnuUndo.Enabled = clsScriptActive.CanUndo
        mnuRedo.Enabled = clsScriptActive.CanRedo

        Dim bScriptselected = clsScriptActive.SelectedText.Length > 0
        Dim bScriptExists = clsScriptActive.TextLength > 0

        mnuCut.Enabled = bScriptselected
        mnuCopy.Enabled = bScriptselected
        mnuPaste.Enabled = Clipboard.ContainsData(DataFormats.Text)
        mnuSelectAll.Enabled = bScriptExists
        mnuClear.Enabled = bScriptExists

        mnuRunCurrentLineSelection.Enabled = bScriptExists
        mnuRunAllText.Enabled = bScriptExists

        mnuOpenScriptasFile.Enabled = bScriptExists
        mnuSaveScript.Enabled = bScriptExists

        cmdRunLineSelection.Enabled = bScriptExists
        cmdRunAll.Enabled = bScriptExists
        cmdSave.Enabled = bScriptExists
        cmdClear.Enabled = bScriptExists

        cmdRemoveTab.Enabled = TabControl.TabCount > 1
    End Sub

    Private Sub EnableRunButtons(bEnable As Boolean)
        cmdRunLineSelection.Enabled = bEnable
        cmdRunAll.Enabled = bEnable
    End Sub

    '''--------------------------------------------------------------------------------------------
    ''' <summary>
    '''     If the caret is next to a bracket, then it highlights the paired open/close bracket. 
    '''     If it cannot find a paired bracket, then it displays the bracket next to the caret in 
    '''     the specified error colour. For nested indented brackets, also shows a vertical 
    '''     indentation line. <para>
    '''     This sub is based on a C# function from:
    '''     https://github.com/jacobslusser/ScintillaNET/wiki/Brace-Matching. </para>
    ''' </summary>
    '''--------------------------------------------------------------------------------------------
    Private Sub HighlightPairedBracket()
        'if caret has not moved, then do nothing
        Static iLastCaretPos As Integer = 0
        Dim iCaretPos As Integer = clsScriptActive.CurrentPosition
        If iLastCaretPos = iCaretPos Then
            Exit Sub
        End If
        iLastCaretPos = iCaretPos

        Dim iBracketPos1 As Integer = -1
        'is there a brace to the left Or right?
        If iCaretPos > 0 AndAlso IsBracket(clsScriptActive.GetCharAt(iCaretPos - 1)) Then
            iBracketPos1 = iCaretPos - 1
        ElseIf IsBracket(clsScriptActive.GetCharAt(iCaretPos)) Then
            iBracketPos1 = iCaretPos
        End If

        If iBracketPos1 >= 0 Then
            'find the matching brace
            Dim iBracketPos2 As Integer = clsScriptActive.BraceMatch(iBracketPos1)
            If iBracketPos2 = Scintilla.InvalidPosition Then
                clsScriptActive.BraceBadLight(iBracketPos1)
                clsScriptActive.HighlightGuide = 0
            Else
                clsScriptActive.BraceHighlight(iBracketPos1, iBracketPos2)
                clsScriptActive.HighlightGuide = clsScriptActive.GetColumn(iBracketPos1)
            End If
        Else
            'turn off brace matching
            clsScriptActive.BraceHighlight(Scintilla.InvalidPosition, Scintilla.InvalidPosition)
            clsScriptActive.HighlightGuide = 0
        End If
    End Sub

    '''--------------------------------------------------------------------------------------------
    ''' <summary>   Automatically sets the indent of a new line.<para>
    '''             Normally the indent is set to the same indent as the last previous non-empty 
    '''             line. However, if the caret was between '{}' when enter was pressed, then 
    '''             increases the indent by 2 spaces and moves the '}' to the next line.</para><para>
    '''             This results in code with nested indents. For example:</para>
    '''             <code>
    '''             a = function(b){
    '''             ..statement1
    '''             ..if (b){
    '''             ....statement2
    '''             ..}
    '''             }</code>. </summary>
    '''
    ''' <param name="iKeyPressed">    The last key pressed by the user. </param>
    '''--------------------------------------------------------------------------------------------
    Private Sub InsertIndent(iKeyPressed As Integer)
        ' we only need to enter an indent when the user presses the enter key
        If iKeyPressed <> Keys.Enter Or clsScriptActive.AutoCActive <> False Then
            Exit Sub
        End If

        ' find indent on previous non-blank line
        Dim iIndent As Integer = 0
        Dim strLinePrevText As String = ""
        For iLineNum As Integer = clsScriptActive.CurrentLine - 1 To 0 Step -1
            strLinePrevText = clsScriptActive.Lines(iLineNum).Text
            If Not String.IsNullOrWhiteSpace(strLinePrevText) Then
                iIndent = strLinePrevText.Length - strLinePrevText.TrimStart().Length
                Exit For
            End If
        Next

        ' if caret before '}', then move '}' to new line
        Dim strCharNext As String = If(clsScriptActive.Text.Length > clsScriptActive.CurrentPosition, clsScriptActive.Text(clsScriptActive.CurrentPosition), "")
        If strCharNext = "}"c Then
            clsScriptActive.InsertText(clsScriptActive.CurrentPosition, vbCrLf & "".PadRight(iIndent))
            clsScriptActive.ScrollRange(clsScriptActive.CurrentPosition, clsScriptActive.CurrentPosition + 2) 'ensure '}' is still visible to user
        End If

        ' if caret after '{', then increase indent 
        strLinePrevText = strLinePrevText.Replace(vbCrLf, "").TrimEnd() ' remove carriage returns and trailing spaces
        Dim strCharPrev As String = If(strLinePrevText.Length >= 1, strLinePrevText(strLinePrevText.Length - 1), "")
        If strCharPrev = "{"c Then
            iIndent += 2
        End If

        ' apply indent to current line
        clsScriptActive.InsertText(clsScriptActive.CurrentPosition, "".PadRight(iIndent))

        ' move caret to end indent
        clsScriptActive.GotoPosition(clsScriptActive.CurrentPosition + iIndent)
    End Sub

    '''--------------------------------------------------------------------------------------------
    ''' <summary>
    '''     If <paramref name="charNew"/> is a bracket/quote, then inserts a closing bracket/quote. <para>
    '''     This sub is based on a C# function from:
    '''     https://github.com/jacobslusser/ScintillaNET/wiki/Character-Autocompletion. </para><para>
    '''     It avoids inserting matching quotes in situations such as "don't". 
    '''     It also ensures that the caret does not remain in the center upon multiple quote insertions.</para><para>
    '''     For example ("|" is cursor position; '=&gt;' is output):</para><list type="bullet"><item>
    '''         insert ' =&gt; '|' </item><item>
    '''         insert ' again =&gt; ''| </item></list>
    '''     Visual Studio and RStudio have the same behaviour.
    ''' </summary>
    ''' <param name="charNew">  The character typed by the user. </param>
    '''--------------------------------------------------------------------------------------------
    Private Sub InsertMatchedChars(charNew As Char)
        Dim iCaretPos As Integer = clsScriptActive.CurrentPosition
        Dim bIsDocStart As Boolean = iCaretPos = 1
        Dim bIsDocEnd As Boolean = iCaretPos = clsScriptActive.Text.Length

        Dim charPrev As Char = If(bIsDocStart, Chr(0), ChrW(clsScriptActive.GetCharAt(iCaretPos - 2)))
        Dim charNext As Char = If(bIsDocEnd, Chr(0), ChrW(clsScriptActive.GetCharAt(iCaretPos)))

        Dim dctBrackets As New Dictionary(Of Char, Char) From {{"(", ")"}, {"{", "}"}, {"[", "]"}}

        'If user entered an open bracket character
        If dctBrackets.ContainsKey(charNew) Then
            If IsCharQuote(charNext) Then
                Exit Sub
            End If
            'insert close bracket character
            clsScriptActive.InsertText(iCaretPos, dctBrackets(charNew))
        ElseIf IsCharQuote(charNew) Then ' else if user entered quote
            'if user enters multiple quotes, then ensure that the caret does not remain in the center
            If charPrev = charNew AndAlso charNext = charNew Then
                clsScriptActive.DeleteRange(iCaretPos, 1)
                clsScriptActive.GotoPosition(iCaretPos)
                Exit Sub
            End If

            'in certain situations add a closing quote after the caret
            Dim charClosingBracket As Char
            Dim bIsEnclosedByBrackets As Boolean = dctBrackets.TryGetValue(charPrev, charClosingBracket) AndAlso charNext = charClosingBracket
            Dim bIsEnclosedBySpaces As Boolean = IsCharBlank(charPrev) AndAlso IsCharBlank(charNext)
            Dim bIsEnclosedByBracketAndSpace As Boolean = (dctBrackets.ContainsKey(charPrev) AndAlso IsCharBlank(charNext)) _
                                                   OrElse (dctBrackets.ContainsValue(charNext) AndAlso IsCharBlank(charPrev))
            If bIsEnclosedByBrackets OrElse bIsEnclosedBySpaces OrElse bIsEnclosedByBracketAndSpace Then
                clsScriptActive.InsertText(iCaretPos, charNew)
            End If
        End If
    End Sub

    Private Function IsBracket(iNewChar As Integer) As Boolean
        Dim arrRBrackets() As String = {"(", ")", "{", "}", "[", "]"}
        Return arrRBrackets.Contains(Chr(iNewChar))
    End Function

    Private Function IsCharBlank(charNew As Char) As Boolean
        Return charNew = Chr(0) OrElse String.IsNullOrWhiteSpace(charNew.ToString()) OrElse charNew = vbLf OrElse charNew = vbCr
    End Function

    Private Function IsCharQuote(charNew As Char) As Boolean
        Return charNew = """" OrElse charNew = "'"
    End Function

    Private Sub LoadScript()
        If clsScriptActive.TextLength > 0 _
                AndAlso MsgBox("Loading a script from file will clear your current script" _
                               & Environment.NewLine & "Do you still want to load?",
                               vbYesNo, "Load Script From File") = vbNo Then
            Exit Sub
        End If

        Using dlgLoad As New OpenFileDialog
            dlgLoad.Title = "Load Script From Text File"
            dlgLoad.Filter = "Text & R Script Files (*.txt,*.R)|*.txt;*.R|R Script File (*.R)|*.R|Text File (*.txt)|*.txt"

            'Ensure that dialog opens in correct folder.
            'In theory, we should be able to use `dlgLoad.RestoreDirectory = True` but this does
            'not work (I think a bug in WinForms).So we need to use a static variable instead.
            Static strInitialDirectory As String = frmMain.clsInstatOptions.strWorkingDirectory
            dlgLoad.InitialDirectory = strInitialDirectory

            If Not dlgLoad.ShowDialog() = DialogResult.OK Then
                Exit Sub
            End If

            Try
                frmMain.ucrScriptWindow.clsScriptActive.Text = File.ReadAllText(dlgLoad.FileName)
                TabControl.SelectedTab.Text = System.IO.Path.GetFileName(dlgLoad.FileName)
                strInitialDirectory = Path.GetDirectoryName(dlgLoad.FileName)
                bIsTextChanged = False
            Catch
                MsgBox("Could not load the script from file." & Environment.NewLine &
                       "The file may be in use by another program or you may not have access to write to the specified location.",
                       vbExclamation, "Load Script")
            End Try
        End Using

    End Sub

    Private Function NewScriptEditor() As Scintilla
        Dim clsNewScript As Scintilla = New Scintilla With {
            .ContextMenuStrip = mnuContextScript,
            .Dock = DockStyle.Fill,
            .Lexer = Lexer.R,
            .Location = New Point(3, 3),
            .Name = "txtScriptAdded",
            .Size = New Size(391, 409),
            .TabIndex = 14, 'TODO
            .TabWidth = 2
        }

        clsNewScript.StyleResetDefault()
        clsNewScript.Styles(Style.Default).Font = "Consolas"
        clsNewScript.Styles(Style.Default).Size = 10

        'TODO  Configure from R-Instat options?
        'clsScript.Styles(Style.Default).Font = frmMain.clsInstatOptions.fntEditor.Name
        'clsScript.Styles(Style.Default).Size = frmMain.clsInstatOptions.fntEditor.Size

        ' Instruct the lexer to calculate folding
        clsNewScript.SetProperty("fold", "1")
        clsNewScript.SetProperty("fold.compact", "1")

        ' Configure a margin to display folding symbols
        clsNewScript.Margins(2).Type = MarginType.Symbol
        clsNewScript.Margins(2).Mask = Marker.MaskFolders
        clsNewScript.Margins(2).Sensitive = True
        clsNewScript.Margins(2).Width = 20

        ' Set colors for all folding markers
        For i As Integer = 25 To 31
            clsNewScript.Markers(i).SetForeColor(SystemColors.ControlLightLight)
            clsNewScript.Markers(i).SetBackColor(SystemColors.ControlDark)
        Next

        ' Configure folding markers with respective symbols
        clsNewScript.Markers(Marker.Folder).Symbol = MarkerSymbol.BoxPlus
        clsNewScript.Markers(Marker.FolderOpen).Symbol = MarkerSymbol.BoxMinus
        clsNewScript.Markers(Marker.FolderEnd).Symbol = MarkerSymbol.BoxPlusConnected
        clsNewScript.Markers(Marker.FolderMidTail).Symbol = MarkerSymbol.TCorner
        clsNewScript.Markers(Marker.FolderOpenMid).Symbol = MarkerSymbol.BoxMinusConnected
        clsNewScript.Markers(Marker.FolderSub).Symbol = MarkerSymbol.VLine
        clsNewScript.Markers(Marker.FolderTail).Symbol = MarkerSymbol.LCorner

        ' Enable automatic folding
        clsNewScript.AutomaticFold = AutomaticFold.Show Or AutomaticFold.Click Or AutomaticFold.Change

        clsNewScript.IndentationGuides = IndentView.LookBoth
        clsNewScript.StyleClearAll()
        clsNewScript.Styles(Style.R.Default).ForeColor = Color.Silver
        clsNewScript.Styles(Style.R.Comment).ForeColor = Color.Green
        clsNewScript.Styles(Style.R.KWord).ForeColor = Color.Blue
        clsNewScript.Styles(Style.R.BaseKWord).ForeColor = Color.Blue
        clsNewScript.Styles(Style.R.OtherKWord).ForeColor = Color.Blue
        clsNewScript.Styles(Style.R.Number).ForeColor = Color.Purple
        clsNewScript.Styles(Style.R.String).ForeColor = Color.FromArgb(163, 21, 21)
        clsNewScript.Styles(Style.R.String2).ForeColor = Color.FromArgb(163, 21, 21)
        clsNewScript.Styles(Style.R.Operator).ForeColor = Color.Gray
        clsNewScript.Styles(Style.R.Identifier).ForeColor = Color.Black
        clsNewScript.Styles(Style.R.Infix).ForeColor = Color.Gray
        clsNewScript.Styles(Style.R.InfixEol).ForeColor = Color.Gray
        clsNewScript.Styles(Style.BraceLight).BackColor = Color.LightGray
        clsNewScript.Styles(Style.BraceLight).ForeColor = Color.BlueViolet
        clsNewScript.Styles(Style.BraceBad).ForeColor = Color.Red

        Dim tmp = clsNewScript.DescribeKeywordSets()
        clsNewScript.SetKeywords(0, "if else repeat while function for in next break TRUE FALSE NULL NA Inf NaN NA_integer_ NA_real_ NA_complex_ NA_character")

        'TODO if we want to set the key words for 'default package functions' (key word set 1) 
        ' and/or 'other package functions', then a good list is available at:
        '  https://raw.githubusercontent.com/moltenform/scite-files/master/files/files/api_files/r.properties  

        Return clsNewScript
    End Function

    Private Sub RunCurrentLine()
        Static strScriptCmd As String = "" 'static so that script can be added to with successive calls of this function

        If clsScriptActive.TextLength > 0 Then
            Dim strLineTextString = clsScriptActive.Lines(clsScriptActive.CurrentLine).Text
            strScriptCmd &= vbCrLf & strLineTextString 'insert carriage return to ensure that new text starts on new line
            strScriptCmd = RunText(strScriptCmd)

            Dim iNextLinePos As Integer = clsScriptActive.Lines(clsScriptActive.CurrentLine).EndPosition
            clsScriptActive.GotoPosition(iNextLinePos)
        End If
    End Sub

    Private Function RunText(strText As String) As String
        Return If(Not String.IsNullOrEmpty(strText),
                  frmMain.clsRLink.RunScriptFromWindow(strNewScript:=strText, strNewComment:=strComment),
                  "")
    End Function

    Private Sub SaveScript()
        Using dlgSave As New SaveFileDialog
            dlgSave.Title = "Save Script To File"
            dlgSave.Filter = "R Script File (*.R)|*.R|Text File (*.txt)|*.txt"

            'Ensure that dialog opens in correct folder.
            'In theory, we should be able to use `dlgLoad.RestoreDirectory = True` but this does
            'not work (I think a bug in WinForms).So we need to use a static variable instead.
            Static strInitialDirectory As String = frmMain.clsInstatOptions.strWorkingDirectory
            dlgSave.InitialDirectory = strInitialDirectory

            If dlgSave.ShowDialog() = DialogResult.OK Then
                Try
                    File.WriteAllText(dlgSave.FileName, clsScriptActive.Text)
                    TabControl.SelectedTab.Text = System.IO.Path.GetFileName(dlgSave.FileName)
                    strInitialDirectory = Path.GetDirectoryName(dlgSave.FileName)
                    bIsTextChanged = False
                Catch
                    MsgBox("Could not save the script file." & Environment.NewLine &
                           "The file may be in use by another program or you may not have access to write to the specified location.",
                           vbExclamation, "Save Script")
                End Try
            End If
        End Using
    End Sub

    '''--------------------------------------------------------------------------------------------
    ''' <summary>
    '''     Sets the margin used to display line numbers to the correct width so that line numbers 
    '''     up to and including <paramref name="iMaxLineNumberCharLengthNew"/> display correctly.
    ''' </summary>
    ''' <param name="iMaxLineNumberCharLengthNew"> The maximum line number that needs to be 
    '''     displayed (normally the number of lines in the script).</param>
    ''' <param name="bForce"> If true, then forces reset of margin width even if the margin width  
    '''     is the same as the last time that this sub was called (normally only needed when 
    '''     switching to a new tab).</param>
    '''--------------------------------------------------------------------------------------------
    Private Sub SetLineNumberMarginWidth(iMaxLineNumberCharLengthNew As Integer,
                                         Optional bForce As Boolean = False)
        If iMaxLineNumberCharLength = iMaxLineNumberCharLengthNew _
                AndAlso Not bForce Then
            Exit Sub
        End If
        iMaxLineNumberCharLength = iMaxLineNumberCharLengthNew

        Dim strLineNumber As String = "9"
        For i As Integer = 1 To iMaxLineNumberCharLength
            strLineNumber &= "9"
        Next
        clsScriptActive.Margins(0).Width = clsScriptActive.TextWidth(Style.LineNumber, strLineNumber)
    End Sub

    Private Sub clsScriptActive_CharAdded(sender As Object, e As CharAddedEventArgs) Handles clsScriptActive.CharAdded
        InsertMatchedChars(ChrW(e.Char))
        InsertIndent(e.Char)
    End Sub

    Private Sub clsScriptActive_TextChanged(sender As Object, e As EventArgs) Handles clsScriptActive.TextChanged
        bIsTextChanged = True
        EnableDisableButtons()
        SetLineNumberMarginWidth(clsScriptActive.Lines.Count.ToString().Length)
    End Sub

    Private Sub clsScriptActive_UpdateUI(sender As Object, e As UpdateUIEventArgs) Handles clsScriptActive.UpdateUI
        HighlightPairedBracket()
    End Sub

    Private Sub cmdAddTab_Click(sender As Object, e As EventArgs) Handles cmdAddTab.Click
        addTab()
    End Sub

    Private Sub cmdLoadScript_Click(sender As Object, e As EventArgs) Handles cmdLoadScript.Click
        LoadScript()
    End Sub

    Private Sub cmdRemoveTab_Click(sender As Object, e As EventArgs) Handles cmdRemoveTab.Click
        'never remove last tab
        If TabControl.TabCount < 2 Then
            Exit Sub
        End If

        If clsScriptActive.TextLength > 0 AndAlso bIsTextChanged _
            AndAlso MsgBox("Are you sure you want to delete the tab and lose the contents?",
                               vbYesNo, "Remove Tab") = vbNo Then
            Exit Sub
        End If

        Dim iTabRemovedIndex As Integer = TabControl.TabPages.IndexOf(TabControl.SelectedTab)
        Dim iTabNewSelected As Integer = iTabRemovedIndex
        If iTabRemovedIndex >= TabControl.TabCount - 1 Then
            iTabNewSelected -= 1
        End If
        TabControl.TabPages.Remove(TabControl.SelectedTab)
        TabControl.SelectedTab = TabControl.TabPages(iTabNewSelected)
        EnableDisableButtons()
    End Sub

    Private Sub mnuClearContents_Click(sender As Object, e As EventArgs) Handles mnuClear.Click, cmdClear.Click
        If clsScriptActive.TextLength < 1 _
                OrElse MsgBox("Are you sure you want to clear the contents of the script window?",
                               vbYesNo, "Clear") = vbNo Then
            Exit Sub
        End If
        clsScriptActive.ClearAll()
        EnableDisableButtons()
    End Sub

    Private Sub mnuContextScript_Opening(sender As Object, e As EventArgs) Handles mnuContextScript.Opening
        EnableDisableButtons()
    End Sub

    Private Sub mnuCopy_Click(sender As Object, e As EventArgs) Handles mnuCopy.Click
        CopyText()
    End Sub

    Private Sub mnuCut_Click(sender As Object, e As EventArgs) Handles mnuCut.Click
        CutText()
    End Sub

    Private Sub mnuHelp_Click(sender As Object, e As EventArgs) Handles mnuHelp.Click, cmdHelp.Click
        Help.ShowHelp(Me, frmMain.strStaticPath & "\" & frmMain.strHelpFilePath, HelpNavigator.TopicId, "542")
    End Sub

    Private Sub mnuLoadScript_Click(sender As Object, e As EventArgs) Handles mnuLoadScriptFromFile.Click
        LoadScript()
    End Sub

    Private Sub mnuOpenScriptasFile_Click(sender As Object, e As EventArgs) Handles mnuOpenScriptasFile.Click
        Try
            If Not Directory.Exists(strRInstatLogFilesFolderPath) Then
                Directory.CreateDirectory(strRInstatLogFilesFolderPath)
            End If
            Dim strScriptFilename As String = "RInstatScript.R"
            Dim i As Integer = 0
            While File.Exists(Path.Combine(strRInstatLogFilesFolderPath, strScriptFilename))
                i += 1
                strScriptFilename = "RInstatScript" & i & ".R"
            End While
            File.WriteAllText(Path.Combine(strRInstatLogFilesFolderPath, strScriptFilename),
                              frmMain.clsRLink.GetRSetupScript() & clsScriptActive.Text)
            Process.Start(Path.Combine(strRInstatLogFilesFolderPath, strScriptFilename))
            TabControl.SelectedTab.Text = strScriptFilename
        Catch
            MsgBox("Could not save the script file." & Environment.NewLine &
                   "The file may be in use by another program or you may not have access to write to the specified location.",
                   vbExclamation, "Open Script")
        End Try
    End Sub

    Private Sub mnuPaste_Click(sender As Object, e As EventArgs) Handles mnuPaste.Click
        PasteText()
    End Sub

    Private Sub mnuRedo_Click(sender As Object, e As EventArgs) Handles mnuRedo.Click
        'Determine if last operation can be redone in text box.   
        If clsScriptActive.CanRedo Then
            clsScriptActive.Redo()
            EnableDisableButtons()
        End If
    End Sub

    Private Sub mnuRunAllText_Click(sender As Object, e As EventArgs) Handles mnuRunAllText.Click, cmdRunAll.Click
        If clsScriptActive.TextLength < 1 _
                OrElse MsgBox("Are you sure you want to run the entire contents of the script window?",
                              vbYesNo, "Run All") = vbNo Then
            Exit Sub
        End If

        EnableRunButtons(False) 'temporarily disable the run buttons in case its a long operation
        RunText(clsScriptActive.Text)
        EnableRunButtons(True)
    End Sub

    Private Sub mnuRunCurrentLineSelection_Click(sender As Object, e As EventArgs) Handles mnuRunCurrentLineSelection.Click, cmdRunLineSelection.Click
        'temporarily disable the buttons in case its a long operation
        EnableRunButtons(False)
        If clsScriptActive.SelectedText.Length > 0 Then
            RunText(clsScriptActive.SelectedText)
        Else
            RunCurrentLine()
        End If
        EnableRunButtons(True)
    End Sub

    Private Sub cmdSave_Click(sender As Object, e As EventArgs) Handles cmdSave.Click
        SaveScript()
    End Sub

    Private Sub mnuSaveScript_Click(sender As Object, e As EventArgs) Handles mnuSaveScript.Click
        SaveScript()
    End Sub

    Private Sub mnuSelectAll_Click(sender As Object, e As EventArgs) Handles mnuSelectAll.Click
        SelectAllText()
    End Sub

    Private Sub mnuUndo_Click(sender As Object, e As EventArgs) Handles mnuUndo.Click
        'Determine if last operation can be undone in text box.   
        If clsScriptActive.CanUndo Then
            clsScriptActive.Undo() 'Undo the last operation.
            EnableDisableButtons()
        End If
    End Sub

    Private Sub tabControl_Selected(sender As Object, e As TabControlEventArgs) Handles TabControl.Selected
        Dim tabPageControls = TabControl.SelectedTab.Controls
        For Each control In tabPageControls
            If TypeOf control Is Scintilla Then
                clsScriptActive = DirectCast(control, Scintilla)

                'If tab contains script, then assume that latest script is not yet saved.
                'This is just to be on the safe side. It is not worth the extra complexity of
                'checking if the script is the same as the associated file name
                bIsTextChanged = clsScriptActive.TextLength > 0

                EnableDisableButtons()
                Exit Sub
            End If
        Next

        If IsNothing(clsScriptActive) Then
            MsgBox("Developer error: could not find editor winfow in tab.")
        End If
    End Sub

    Private Sub ucrScript_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        toolTipScriptWindow.SetToolTip(cmdRunLineSelection, "Run the current line or selection. (Ctrl+Enter)")
        toolTipScriptWindow.SetToolTip(cmdRunAll, "Run all the text in the tab. (Ctrl+Alt+R)")
        toolTipScriptWindow.SetToolTip(cmdLoadScript, "Load a script from file into the current tab.")
        toolTipScriptWindow.SetToolTip(cmdSave, "Save the script in the current tab to a file.")
        toolTipScriptWindow.SetToolTip(cmdAddTab, "Add a new tab.")
        toolTipScriptWindow.SetToolTip(cmdRemoveTab, "Delete the current tab.")
        toolTipScriptWindow.SetToolTip(cmdClear, "Clear the contents of the current tab. (Ctrl+L)")
        toolTipScriptWindow.SetToolTip(cmdHelp, "Display the Script Window help information.")

        mnuUndo.ToolTipText = "Undo the last change. (Ctrl+Z)"
        mnuRedo.ToolTipText = "Redo the last change. (Ctrl+Y)"
        mnuCut.ToolTipText = "Copy the selected text to the clipboard, then delete the text. (Ctrl+X)"
        mnuCopy.ToolTipText = "Copy the selected text to the clipboard. (Ctrl+C)"
        mnuPaste.ToolTipText = "Paste the contents of the clipboard into the current tab. (Ctrl+V)"
        mnuSelectAll.ToolTipText = "Select all the contents of the current tab. (Ctrl+A)"
        mnuClear.ToolTipText = "Clear the contents of the current tab. (Ctrl+L)"
        mnuRunCurrentLineSelection.ToolTipText = "Run the current line or selection. (Ctrl+Enter)"
        mnuRunAllText.ToolTipText = "Run all the text in the tab. (Ctrl+Alt+R)"
        mnuOpenScriptasFile.ToolTipText = "Save file to log folder and open file in external editor."
        mnuLoadScriptFromFile.ToolTipText = "Load script from file into the current tab."
        mnuSaveScript.ToolTipText = "Save the script in the current tab to a file."
        mnuHelp.ToolTipText = "Display the Script Window help information."

        'normally we would do this in the designer, but designer doesn't allow enter key as shortcut
        mnuRunCurrentLineSelection.ShortcutKeys = Keys.Enter Or Keys.Control
        addTab()
    End Sub

End Class
