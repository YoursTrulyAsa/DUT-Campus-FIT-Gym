document.addEventListener("DOMContentLoaded", function () {

    const startScanner =
        document.getElementById("startScanner");

    const closeScanner =
        document.getElementById("closeScanner");

    const scannerSection =
        document.getElementById("scannerSection");

    const scannerStatus =
        document.getElementById("scannerStatus");

    let qrScanner = null;


    // =========================================================
    // START SCANNER
    // =========================================================

    if (startScanner) {

        startScanner.addEventListener("click", function () {

            scannerSection.style.display = "block";

            startScanner.disabled = true;

            scannerStatus.textContent =
                "Opening camera...";


            qrScanner =
                new Html5Qrcode("qr-reader");


            const config = {

                fps: 10,

                qrbox: {
                    width: 250,
                    height: 250
                },

                formatsToSupport: [
                    Html5QrcodeSupportedFormats.QR_CODE
                ]

            };


            qrScanner.start(

                {
                    facingMode: "environment"
                },

                config,


                // =================================================
                // QR CODE DETECTED
                // =================================================

                function (decodedText) {

                    scannerStatus.textContent =
                        "QR code detected. Verifying check-in...";


                    qrScanner.stop()
                        .then(function () {

                            verifyQrCode(decodedText);

                        })
                        .catch(function () {

                            verifyQrCode(decodedText);

                        });

                },


                // =================================================
                // SCANNING ERROR
                // =================================================

                function () {

                    // Normal scanning failures are ignored.

                }

            )
                .catch(function (error) {

                    console.error(error);

                    scannerStatus.textContent =
                        "Unable to access your camera.";

                    startScanner.disabled = false;

                });

        });

    }


    // =========================================================
    // CLOSE SCANNER
    // =========================================================

    if (closeScanner) {

        closeScanner.addEventListener("click", function () {

            if (qrScanner) {

                qrScanner.stop()
                    .then(function () {

                        qrScanner.clear();

                        qrScanner = null;

                    })
                    .catch(function () {

                        qrScanner = null;

                    });

            }

            scannerSection.style.display = "none";

            startScanner.disabled = false;

            scannerStatus.textContent =
                "Position the QR code inside the scanner.";

        });

    }


    // =========================================================
    // SEND QR CODE TO MEMBER CONTROLLER
    // =========================================================

    function verifyQrCode(qrData) {

        const form =
            document.createElement("form");

        form.method = "POST";

        form.action =
            "/Member/VerifyQrCheckIn";


        // =====================================================
        // ANTI-FORGERY TOKEN
        // =====================================================

        const token =
            document.querySelector(
                'input[name="__RequestVerificationToken"]'
            );


        if (token) {

            const tokenInput =
                document.createElement("input");

            tokenInput.type = "hidden";

            tokenInput.name =
                "__RequestVerificationToken";

            tokenInput.value =
                token.value;

            form.appendChild(tokenInput);

        }


        // =====================================================
        // QR DATA
        // =====================================================

        const qrInput =
            document.createElement("input");

        qrInput.type = "hidden";

        qrInput.name = "qrData";

        qrInput.value = qrData;

        form.appendChild(qrInput);


        // =====================================================
        // SUBMIT
        // =====================================================

        document.body.appendChild(form);

        form.submit();

    }

});