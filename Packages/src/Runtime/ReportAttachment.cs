using System;
using System.Text;
using UnityEngine;

namespace oojjrs.ore
{
    public sealed class ReportAttachment
    {
        public string ContentType { get; }
        public byte[] Data { get; }
        public string FileName { get; }

        public ReportAttachment(string fileName, byte[] data, string contentType = "application/octet-stream")
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("The attachment file name is required.", nameof(fileName));
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("The attachment content type is required.", nameof(contentType));

            ContentType = contentType;
            Data = data;
            FileName = fileName;
        }

        public static ReportAttachment CreateJpeg(string fileName, Texture2D texture, int quality = 75)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));
            if ((quality < 1) || (quality > 100))
                throw new ArgumentOutOfRangeException(nameof(quality));

            return new ReportAttachment(fileName, texture.EncodeToJPG(quality), "image/jpeg");
        }

        public static ReportAttachment CreatePng(string fileName, Texture2D texture)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            return new ReportAttachment(fileName, texture.EncodeToPNG(), "image/png");
        }

        public static ReportAttachment CreateText(string fileName, string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            return new ReportAttachment(fileName, Encoding.UTF8.GetBytes(text), "text/plain; charset=utf-8");
        }
    }
}
