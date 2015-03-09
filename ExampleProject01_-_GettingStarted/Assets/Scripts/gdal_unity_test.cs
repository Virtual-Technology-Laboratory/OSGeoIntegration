/*
 * Copyright (c) 2014, Roger Lew (rogerlew.gmail.com)
 * Date: 2/5/2015
 * License: BSD (3-clause license)
 * 
 * The project described was supported by NSF award number IIA-1301792
 * from the NSF Idaho EPSCoR Program and by the National Science Foundation.
 * 
 */

using System;
using System.IO;
// using System.Runtime.InteropServices;

using UnityEngine;
using System.Collections;

using OSGeo.GDAL;
using OSGeo.OSR;

using ProjApi;

public class gdal_unity_test : MonoBehaviour {

	// Use this for initialization
	void Start () {
        Gdal.AllRegister();
        Debug.Log("All registered");

        Dataset ds = Gdal.Open(@".\Assets\Resources\DryCreek.FBFM40.TL8.tif", Access.GA_ReadOnly);

        Debug.Log("Dataset Opened");
        Debug.Log(string.Format("{0}, {1}", ds.RasterXSize, ds.RasterXSize));

        double[] geoTransform = new double[6];
        ds.GetGeoTransform(geoTransform);

        Debug.Log(geoTransform[0]);
        Debug.Log(geoTransform[1]);
        Debug.Log(geoTransform[2]);
        Debug.Log(geoTransform[3]);
        Debug.Log(geoTransform[4]);
        Debug.Log(geoTransform[5]);

        string projWkt = ds.GetProjection();

        Debug.Log(projWkt);

        SpatialReference sr = new SpatialReference(projWkt);
        string proj4;
        int status = sr.ExportToProj4(out proj4);

        Debug.Log(status);
        Debug.Log(proj4);

        double[] x = { geoTransform[0] };
        double[] y = { geoTransform[3] };

        Projection src = new Projection(proj4);
        Projection dst = new Projection(@"+proj=latlong +datum=WGS84");

        Projection.Transform(src, dst, x, y, null);

        Debug.Log(x[0]);
        Debug.Log(y[0]);

	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
