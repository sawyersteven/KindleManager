## IBook interface:

All book formats must inherit IBook. This is a basic collection of props that
mirror the data in the database row for each book. This is loosely based on
standard fields found in an EPUB metadata OPF.

Dates are respresented as "yyyy-MM-dd" eg "1999-12-31""

Methods required:
	void WriteMetada()
		Writes all metadata to disk. This includes everything except text content
		and images.
		For an EPUB this effectively just creates or updates an OPF.
		For a MOBI-like file this writes all headers up to the text content.

	string TextContent()
		A single HTML document containing all text and styles. Chapters are to be
		separated by mobi-standard <mbp:pagebreak/> nodes.
		TOC information can be included in markup by giving nodes a "toclabel"
			attribute. This will then be converted in order into a toc when
			converted to another format.
		Img sources should be formatted as "00001.jpg" with 00001 referring to the
		first image in Images().
		
	byte[][] Images()
		Binary contents of all images using order described above.