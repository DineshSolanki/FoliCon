INDX( 	 #           (   8   č       U                                   ´    ­ņĄj˛×ô˛×ô˛×ô˛×                      s c . u s e r _ p a g e d . 1 . e t l               ´    ­ņĄj˛×ô˛×ô˛×ô˛×                      S C U S E R ~ 2 . E T L                     ´   	 p Z     ´    ­ņĄj˛×ô˛×ô˛×ô˛×                      S C U S E R ~ 2 . E T L                     ´    ­ņĄj˛×ô˛×ô˛×ô˛×                      S C  S E R ~ 2 . E T L                     ô˛×ô˛×ô˛×                      S C U S E R ~ 2 . E T L                      To emphasize removal of 1080i, closing bracket etc, but not needed due to the last part
            // .* --Remove all trailing information after having found year or resolution as junk usually follows
            var cleanTitle = Regex.Replace(normalizedTitle, @"\s*\(?((\d{4})|(420)|(720)|(1080))p?i?\)?.*", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
           cleanTitle = Regex.Replace(cleanTitle, @"\[.*\]", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            cleanTitle = Regex.Replace(cleanTitle, " {2,}", " ");
            return string.IsNullOrWhiteSpace(cleanTitle) ? normalizedTitle : cleanTitle;
        }
    }
}