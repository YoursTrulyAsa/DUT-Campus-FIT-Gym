document.addEventListener("DOMContentLoaded", function () {

    const startScanner = document.getElementById("startScanner");
    const closeScanner = document.getElementById("closeScanner");
    const scannerSection = document.getElementById("scannerSection");
    const scannerStatus = document.getElementById("scannerStatus");

    let qrScanner = null;
    let scannerRunning = false;
    let scanProcessed = false;


    // ==============================
    // START SCANNER
    // ==============================

    startScanner.addEventListener("click", async function () {

        scannerSection.style.display = "block";

        scannerStatus.textContent =
            "Starting camera...";

        scanProcessed = false;

        qrScanner = new Html5Qrcode("qr-reader");

        try {

            await qrScanner.start(

                { facingMode: "environment" },

                {
                    fps: 10,
                    qrbox: {
                        width: 250,
                        height: 250
                    }
                },

                async function (decodedText) {

                    // Prevent multiple scans
                    if (scanProcessed) {
                        return;
                    }

                    scanProcessed = true;

                    console.log("QR Code:", decodedText);

                    scannerStatus.innerHTML =
                        "<strong>QR CODE SCANNED</strong><br>" +
                        "Verifying access...";

                    scannerStatus.classList.add("success");


                    // Stop camera
                    await stopScanner();


                    // ==============================
                    // SEND QR DATA TO CONTROLLER
                    // ==============================

                    const form = document.createElement("form");

                    form.method = "POST";

                    form.action =
                        "/Member/VerifyQrCheckIn";


                    // Anti-forgery token

                    const token =
                        document.querySelector(
                            '#scannerSection input[name="__RequestVerificationToken"]'
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


                    // QR DATA

                    const qrInput =
                        document.createElement("input");

                    qrInput.type = "hidden";

                    qrInput.name = "qrData";

                    qrInput.value = decodedText;

                    form.appendChild(qrInput);


                    document.body.appendChild(form);


                    // Submit verification

                    form.submit();

                },

                function (errorMessage) {

                    // Normal scanning errors are ignored.

                }

            );

            scannerRunning = true;

            scannerStatus.textContent =
                "Position the QR code inside the scanner.";

        }
        catch (error) {

            console.error(error);

            scannerStatus.textContent =
                "Unable to access the camera. Please allow camera permissions.";

        }

    });


    // ==============================
    // STOP SCANNER
    // ==============================

    async function stopScanner() {

        if (qrScanner && scannerRunning) {

            try {

                await qrScanner.stop();

                qrScanner.clear();

            }
            catch (error) {

                console.error(
                    "Error stopping scanner:",
                    error
                );

            }

            scannerRunning = false;
        }
    }


    // ==============================
    // CLOSE SCANNER
    // ==============================

    closeScanner.addEventListener("click", async function () {

        await stopScanner();

        scannerSection.style.display = "none";

        scannerStatus.textContent =
            "Position the QR code inside the scanner.";

        scannerStatus.classList.remove("success");

        scanProcessed = false;

    });

});