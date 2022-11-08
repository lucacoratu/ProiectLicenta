#Start all the vscode instances for the services for easier developing
$services = @("AccountService", "ChatService", "FriendRequestService", "FriendService", "Gateway")
foreach($service in $services){
    Write-Host "Starting vscode for $service"
    Start-Process -FilePath "code" -ArgumentList "." -WorkingDirectory "./$service/src/" -WindowStyle "hidden"
}

#region Start AccountService vscode
#Start-Process -FilePath "code" -ArgumentList "." -WorkingDirectory "./AccountService/src/" -WindowStyle "minimized"
#endregion

#region Start ChatService vscode
#Start-Process -FilePath "code" -ArgumentList "." -WorkingDirectory "./ChatService/src/" -WindowStyle "minimized"
#endregion

#region Start FriendRequestService vscode
#Start-Process -FilePath "code" -ArgumentList "." -WorkingDirectory "./FriendRequestService/src/" -WindowStyle "minimized"
#endregion

#region Start FriendService vscode
#Start-Process -FilePath "code" -ArgumentList "." -WorkingDirectory "./FriendService/src/" -WindowStyle "minimized"
#endregion

#region Start Gateway vscode
#Start-Process -FilePath "code" -ArgumentList "." -WorkingDirectory "./Gateway/src/" -WindowStyle "minimized"
#endregion