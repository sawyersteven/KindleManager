## IBook interface:

All book formats must inherit IBook. This is a basic collection of props that
mirror the data in the database row for each book. This is loosely based on
standard fields found in an EPUB metadata OPF.

Date strings are respresented as "yyyy-MM-dd" eg "1999-12-31""
Date ints are counted as seconds from Jan 1, 1904.