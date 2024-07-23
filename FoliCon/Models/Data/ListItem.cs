﻿using FoliCon.Modules.utils;

namespace FoliCon.Models.Data;

public class ListItem : BindableBase
{
    private string _title;
    private string _year;
    private string _rating;
    private string _folder;
    private string _overview;
    private string _poster;
    private string _trailerKey;
    private Uri _trailerLink;
    private string _id;
    private MediaType _mediaType;
    
    private string _initialPoster;
    private bool _isInitialSet;

    public bool CanResetPoster
    {
        get => _isInitialSet;
        set => SetProperty(ref _isInitialSet, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Year
    {
        get => _year;
        set => SetProperty(ref _year, value);
    }

    public string Rating
    {
        get => _rating;
        set
        {
            if (double.TryParse(value, out var ratingValue))
            {
                SetProperty(ref _rating, DataUtils.FormatRating(ratingValue)); 
            }
            else
            {
                SetProperty(ref _rating, value);
            }
        }
    }

    public string Folder
    {
        get => _folder;
        set => SetProperty(ref _folder, value);
    }

    public string Overview
    {
        get => _overview;
        set => SetProperty(ref _overview, value);
    }

    public string Poster
    {
        get => _poster;
        set => SetProperty(ref _poster, value);
    }

    public Uri Trailer
    {
        get => _trailerLink;
        set => SetProperty(ref _trailerLink, value);
    }

    public string TrailerKey
    {
        get => _trailerKey;
        set => SetProperty(ref _trailerKey, value);
    }

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    
    public MediaType MediaType
    {
        get => _mediaType;
        set => SetProperty(ref _mediaType, value);
    }

    public ListItem(string title, string year, string rating, string overview = null, string poster = null,
        string folder = "", string id = "", MediaType mediaType = MediaType.Unknown)
    {
        Title = title;
        Year = year;
        Rating = rating;
        Overview = overview;
        Poster = poster;
        Folder = folder;
        Id = id;
        MediaType = mediaType;
    }

    public ListItem()
    {
    }
    
    public void ResetInitialPoster()
    {
        if (!_isInitialSet)
        {
            return;
        }

        Poster = _initialPoster;
        CanResetPoster = false;
    }
    public void SetInitialPoster()
    {
        if (_isInitialSet)
        {
            return;
        }

        _initialPoster = _poster;
        CanResetPoster = true;
    }
}