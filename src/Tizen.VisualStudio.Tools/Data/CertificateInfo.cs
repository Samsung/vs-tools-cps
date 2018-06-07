/*
 * Copyright 2017 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * 	http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tizen.VisualStudio.Tools.Data
{
    public class CertificateInfo
    {
        private string authorCert;
        private string authorPass;
        private string distribCert;
        private string distribPass;

        public string AuthorCertificateFile
        {
            set { this.authorCert = value;  }
            get { return this.authorCert; }
        }

        public string AuthorPassword
        {
            set { this.authorPass = value; }
            get { return this.authorPass; }
        }

        public string DistributorCertificateFile
        {
            set { this.distribCert = value;  }
            get { return this.distribCert; }
        }

        public string DistributorPassword
        {
            set { this.distribPass = value; }
            get { return this.distribPass;  }
        }

        public CertificateInfo()
        {
            this.authorCert = string.Empty;
            this.authorPass = string.Empty;
            this.distribCert = string.Empty;
            this.distribPass = string.Empty;
        }

        public void SetCertificateInfo(string authorFile, string authorPassword,
                                    string distFile, string distPassword)
        {

            this.AuthorCertificateFile = authorFile;
            this.AuthorPassword = authorPassword;
            this.DistributorCertificateFile = distFile;
            this.DistributorPassword = distPassword;
        }
    }
}
