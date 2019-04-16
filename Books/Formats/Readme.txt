## IBook interface:

All book formats must inherit IBook. This is a basic collection of props that
mirror the data in the database row for each book. This is loosely based on
standard fields found in an EPUB metadata OPF.

Dates are respresented as "yyyy-MM-dd" eg "1999-12-31""

Methods required:
	WriteMetada()
		Writes all metadata to disk. This includes everything except text content and images.
		For an EPUB this effectively just creates or updates an OPF.
		For a MOBI-like file this writes all headers up to the text content.

	WriteContent()
		Writes all other content to disk. Content editing is not in the scope of this
		application so this should be used when converting formats.
