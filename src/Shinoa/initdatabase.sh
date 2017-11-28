#!/bin/bash
timestamp() {
  date +"%y-%m-%d_%H-%M-%S"
}

echo Databases/*Context.cs |
tr ' ' '\n' |
sed -r 's_Databases/(\S+)Context.cs_\1_' | 
grep -v 'IDatabase' | 
while read i; do 
  dotnet ef migrations add -v -c "${i}Context" "${i}_$(timestamp)"
  dotnet ef database update -v -c "${i}Context"
done
