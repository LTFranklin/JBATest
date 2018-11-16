JBA Test Project to take a input, transform it and input it into a table.
 
 Chosen language is c#, using SQL for the database side of things. 
 A connection string to a database is required to be entered first in order for the program to function.
 
 Regular expressions are used for parsing the intial file to get the data contianed
 
 Specification was unclear as to whether the name should be obtained from within the file or from user input, but as it states that its an option i have implemented it as an input
 File path is currently set to within the debug folder with a specific file name
 
 Due to time constriants, minimal error checking is implemented, though in a full project it would be included (such as ensuring the name can't be an SQL command, check that the connection string is correct, checking if the table exists already, ect)
 For a similar reason the program may not scale entirely with different data sets with more values/different format