﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Kinect
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Kinect;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [Ignore]
    public class MeshTests : IDisposable
    {
        private KinectSensor sensor;
        private IKinectCalibration calibration = null;
        private Shared<Microsoft.Psi.Imaging.Image> lastImage = null;
        private Shared<Microsoft.Psi.Imaging.Image> lastColor = null;
        private bool disposed = false;

        public void SetupKinect()
        {
            using (var pipeline = Pipeline.Create())
            {
                this.sensor = new KinectSensor(pipeline, new KinectSensorConfiguration() { OutputCalibration = true, OutputBodies = true, OutputColor = true });
                var calibration = this.sensor.Calibration.Do((kc) => this.calibration = kc.DeepClone());

                pipeline.Run(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        [Timeout(60000)]
        [Ignore]
        public void GenerateTexturedMappedMesh()
        {
            // Setup Kinect
            using (var pipeline = Pipeline.Create())
            {
                this.sensor = new KinectSensor(pipeline, new KinectSensorConfiguration() { OutputCalibration = true, OutputBodies = true, OutputColor = true, OutputDepth = true });
                var calibration = this.sensor.Calibration.Do((kc) => this.calibration = kc.DeepClone());
                this.sensor.ColorImage.Do((image) =>
                {
                    if (this.lastColor == null)
                    {
                        this.lastColor = image.AddRef();
                    }
                });
                var c = this.sensor.DepthImage.Do((image) =>
                {
                    if (this.lastImage == null)
                    {
                        this.lastImage = image.AddRef();
                    }
                    if (this.lastImage != null && this.lastColor != null && this.calibration != null)
                    {
                        var mesh = Test.Psi.Kinect.Mesh.MeshFromDepthMap(this.lastImage, this.lastColor, this.calibration);
                        int faceCount = 0;
                        foreach (var face in mesh.Faces)
                        {
                            if (face.Valid)
                            {
                                faceCount++;
                            }
                        }
                        bool writePLY = false;
                        if (writePLY)
                        {
                            string temppath = System.IO.Path.GetTempPath();
                            string fn = temppath + @"\Mesh-New-" + DateTime.Now.ToString("MM-dd-yy.HH.mm.ss") + ".ply";
                            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fn))
                            {
                                file.WriteLine("ply");
                                file.WriteLine("format ascii 1.0");
                                file.WriteLine("element vertex " + mesh.NumberVertices.ToString());
                                file.WriteLine("property float x");
                                file.WriteLine("property float y");
                                file.WriteLine("property float z");
                                file.WriteLine("property uchar red");
                                file.WriteLine("property uchar green");
                                file.WriteLine("property uchar blue");
                                file.WriteLine("element face " + faceCount.ToString());
                                file.WriteLine("property list uchar int vertex_indices");
                                file.WriteLine("end_header");
                                for (int i = 0; i < mesh.NumberVertices; i++)
                                {
                                    file.WriteLine(
                                        string.Format(
                                            "{0:f2} {1:f2} {2:f2} {3:d} {4:d} {5:d}",
                                            mesh.Vertices[i].Pos.X,
                                            mesh.Vertices[i].Pos.Y,
                                            mesh.Vertices[i].Pos.Z,
                                            (int)mesh.Vertices[i].Color.X,
                                            (int)mesh.Vertices[i].Color.Y,
                                            (int)mesh.Vertices[i].Color.Z));
                                }
                                for (int i = 0; i < mesh.NumberFaces; i++)
                                {
                                    if (mesh.Faces[i].Valid)
                                    {
                                        file.Write("3 ");
                                        int edgeIndex = mesh.Faces[i].Edge;
                                        file.Write(mesh.Edges[edgeIndex].Head.ToString() + " ");
                                        edgeIndex = mesh.Edges[edgeIndex].Cw;
                                        file.Write(mesh.Edges[edgeIndex].Head.ToString() + " ");
                                        edgeIndex = mesh.Edges[edgeIndex].Cw;
                                        file.WriteLine(mesh.Edges[edgeIndex].Head.ToString());
                                    }
                                }
                            }
                        }
                    }
                });
                pipeline.Run(TimeSpan.FromSeconds(10));
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.sensor != null)
                    {
                        this.sensor.Dispose();
                        this.sensor = null;
                    }

                    if (this.lastColor != null)
                    {
                        this.lastColor.Dispose();
                        this.lastColor = null;
                    }

                    if (this.lastImage != null)
                    {
                        this.lastImage.Dispose();
                        this.lastImage = null;
                    }
                }

                this.disposed = true;
            }
        }
    }
}
