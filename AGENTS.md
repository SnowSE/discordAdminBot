do not include useless comments, if the function names are not adiquate to explain the functionality, use better names or split into more functions

never use one letter variables unless working in single line lambda functions. variable names should describe their purpose

use records for classes that only hold data

prefer functional methods when working with lists of data

do not make architecture decisions before getting approval from the user

never use the null forgiveness operator or do inappropriate casting. throw exceptions if the data is in an inappropriate state

make sure all exception messages are unique to aid in future codebase searches to find the line that threw the error. never re-use exact exception messages, they can mean the same thing, but the text should be distinct and indicate what was happening that caused the error. do not include direct function names because those may change later. include relevant ids and names with enough information that the error can be reproduced


treat build warnings as build errors, if you are unsure about the correct way to solve build warnings ask the user for guidance