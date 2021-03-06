﻿Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Reflection
Imports System.Text
Imports System.Windows.Forms
Imports DevExpress.XtraRichEdit
Imports DevExpress.XtraRichEdit.API.Native
Imports DevExpress.XtraTreeList.Nodes
Imports DevExpress.XtraScheduler

Namespace SchedulerControlEventViewer
    Partial Public Class Form1
        Inherits DevExpress.XtraBars.Ribbon.RibbonForm

        #Region "Fields"
        Private hooks As List(Of SchedulerEventHooker)
        Private unsafeEvents As New List(Of String)() From {"ContextButtonCustomize", "RangeControlAutoAdjusting", "AppointmentViewInfoCustomizing", "InitAppointmentDisplayText", "InitAppointmentImages"}
        #End Region

        Public Sub New()
            hooks = New List(Of SchedulerEventHooker)()
            InitializeComponent()
            PrepareContent()
            EnumerateEvents()
            checkAllEdit.CheckState = CheckState.Checked
            UnCheckFrequentEvents()

        End Sub

        #Region "static helper functions"
        Private Shared Function SortHooksByName(ByVal arg1 As SchedulerEventHooker, ByVal arg2 As SchedulerEventHooker) As Integer
            Return arg1.Name.CompareTo(arg2.Name)
        End Function
        #End Region

        Private Sub EnumerateEvents()
            Dim events1 = HookEvents(schedulerControl1, GetType(SchedulerControl))
            Dim events2 = HookEvents(schedulerStorage1, GetType(SchedulerStorageBase))
            Dim events = events1.Concat(events2)

            hooks.Sort(AddressOf SortHooksByName)
            For i As Integer = 0 To hooks.Count - 1
                Dim eventName As String = hooks(i).Name
                eventsCheckedListBox.Items.Add(i, eventName)
            Next i
        End Sub

        Private Function HookEvents(ByVal control As Object, ByVal controlType As Type) As EventInfo()
            If controlType Is GetType(SchedulerStorage) Then
                controlType = GetType(SchedulerStorageBase)
            End If
            Dim events() As EventInfo = controlType.GetEvents(BindingFlags.Instance Or BindingFlags.Public Or BindingFlags.DeclaredOnly)
            For Each item As EventInfo In events
                If Not unsafeEvents.Contains(item.Name) Then
                    Dim eventName As String = item.Name
                    hooks.Add(New SchedulerEventHooker(eventName, control, Me))
                End If
            Next item
            Return events
        End Function


        Private Sub PrepareContent()
        End Sub

        Private Function GetEventParameter(ByVal item As PropertyInfo, ByVal value As Object) As String
            Dim strBuilder As New StringBuilder()
            strBuilder.Append(item.Name)
            If item.PropertyType Is GetType(String) Then
                strBuilder.Append("='")
                strBuilder.Append(value)
                strBuilder.Append("'"c)
            Else
                strBuilder.Append("="c)
                strBuilder.Append(value)
            End If
            Return strBuilder.ToString()
        End Function

        Public Sub InitializeEventLogger(ByVal eventName As String, ByVal sender As Object, ByVal args As EventArgs)
            Dim argsType As Type = args.GetType()
            Dim name As String = eventName
            Dim argsTypeName As String = argsType.Name
            Dim propInfoArray() As PropertyInfo = argsType.GetProperties()
            Dim rootListNode As TreeListNode = Nothing
            Dim firstLevelNode As TreeListNode = loggerControl.AppendNode(New Object() { name, argsTypeName }, rootListNode)
            For Each item As PropertyInfo In propInfoArray
                Dim value As String = GetEventParameter(item, item.GetValue(args, Nothing))
                Dim secondLevelNode As TreeListNode = loggerControl.AppendNode(New Object() { "", "", value }, firstLevelNode)
            Next item
            If appendExpandedBox.Checked AndAlso (propInfoArray.Length > 0) Then
                firstLevelNode.ExpandAll()
            End If
            loggerControl.MakeNodeVisible(firstLevelNode)
        End Sub


        #Region "FormControls"
        Private Sub eventsCheckedListBox_ItemCheck(ByVal sender As Object, ByVal e As DevExpress.XtraEditors.Controls.ItemCheckEventArgs) Handles eventsCheckedListBox.ItemCheck
            Dim itemIndex As Integer = e.Index
            Dim state As Boolean = e.State = CheckState.Checked
            Dim hookIndex As Integer = Convert.ToInt32(eventsCheckedListBox.Items(itemIndex).Value)
            If state Then
                hooks(hookIndex).HookEvent()
                checkAllEdit.CheckState = If(eventsCheckedListBox.CheckedItemsCount = eventsCheckedListBox.ItemCount, CheckState.Checked, CheckState.Indeterminate)
            Else
                hooks(hookIndex).UnhookEvent()
                checkAllEdit.CheckState = If(eventsCheckedListBox.CheckedItemsCount = 0, CheckState.Unchecked, CheckState.Indeterminate)
            End If
        End Sub

        Private Sub checkAllEdit_CheckStateChanged(ByVal sender As Object, ByVal e As EventArgs) Handles checkAllEdit.CheckStateChanged
            Dim state As CheckState = checkAllEdit.CheckState
            Select Case state
                Case CheckState.Checked
                    eventsCheckedListBox.CheckAll()
                Case CheckState.Unchecked
                    eventsCheckedListBox.UnCheckAll()
            End Select
        End Sub

        Private Sub expandButtonItem_ItemClick(ByVal sender As Object, ByVal e As DevExpress.XtraBars.ItemClickEventArgs) Handles expandButtonItem.ItemClick
            loggerControl.ExpandAll()
        End Sub

        Private Sub collapseButtonItem_ItemClick(ByVal sender As Object, ByVal e As DevExpress.XtraBars.ItemClickEventArgs) Handles collapseButtonItem.ItemClick
            loggerControl.CollapseAll()
        End Sub

        Private Sub clearButtonItem_ItemClick(ByVal sender As Object, ByVal e As DevExpress.XtraBars.ItemClickEventArgs) Handles clearButtonItem.ItemClick
            loggerControl.ClearNodes()
        End Sub

        Private Sub UnCheckFrequentEvents()
            For i As Integer = 0 To eventsCheckedListBox.Items.Count - 1
                If eventsCheckedListBox.Items(i).Description.Contains("Draw") Then
                    eventsCheckedListBox.Items(i).CheckState = CheckState.Unchecked
                End If
                If eventsCheckedListBox.Items(i).Description.Contains("Query") Then
                    eventsCheckedListBox.Items(i).CheckState = CheckState.Unchecked
                End If
            Next i
            checkAllEdit.CheckState = CheckState.Indeterminate
        End Sub
        #End Region ' FormControls
    End Class
End Namespace
