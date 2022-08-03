$action = New-ScheduledTaskAction -Execute "C:\CDI\EmployeeAlertPopup\AlertFormV2.exe"
$trigger = New-ScheduledTaskTrigger -AtLogOn
$trigger.Repetition = (New-ScheduledTaskTrigger -once -at "12am" -RepetitionInterval (New-TimeSpan -Minutes 5)).repetition
$TaskPrincipal = New-ScheduledTaskPrincipal -GroupId "BUILTIN\Users" -RunLevel Highest
Register-ScheduledTask -TaskName "Alert Popup" -Trigger $trigger -Action $action -Principal $TaskPrincipal
Start-ScheduledTask -TaskName "Alert Popup"