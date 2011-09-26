Features:
1. Collect WMI data (hardware: CPU, RAM, platform; OS, IP, ErrorLogs, ...)
2. Collect CPU usage, Disk Avg. queue, ...
3. Run remote command (run installations and get error code, "rerun" option)
4. Run remote task and gat output: ping, traceroute, ...
5. Wake-on-Lan


Structure:
1. Server, Management Console (with GUI): 
  2 processes: host and GUI:
   a. clients list with online/offline status
   b. menu
   c. Push deployment (in loop (maybe multicast?))
   d. Tcp/XML
   e. Reboot, shutdown,... 
   
   
2. Cleint: 
   a. System Tray Icon with menu (update server, check tasks on server, view log, start/stop, exit) and status.
   b. INI file with all settings (server, port)
   c. Ask for servers connection data on first use.
   d. Keep-Alive message each 30 sec (starting from starting). If client goes down - send "offline" msg.
   e. Sends HW/OS inventory on Startup

3. Metadata:
   XML language.
   commands like "GETTIME" ...


Tasks:
1. Commands and classes (metadata)
2. TCP stack
3. XML parser
4. Client


Bonus:
1. Important tasks wake up computer and run task.
2. Android, Linux clients.
3. 

