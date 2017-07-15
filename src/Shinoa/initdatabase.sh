for f in Databases/*Context.cs; do sed -r 's_Databases/(\S+)Context.cs_\1_' <<< "$f"; done 
| grep -v 'IDatabase' | while read i; do 
dotnet ef migrations add -v -c "${i}Context" "$i"
dotnet ef database update -v -c "${i}Context"; done