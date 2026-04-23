module.exports = async function(context) {
    if (!context || !context.request || !context.request.body) {
        return {
            status: 400,
            body: { error: "Invalid request structure" }
        };
    }

    const input = context.request.body;

    const headers = context.request.headers || {};

    if (headers['x-atria-system-execute'] === 'ping') {
        return {
            status: 200,
            body: { status: "ready", timestamp: new Date().toISOString() }
        };
    }


    if (typeof main !== 'function') {
        return {
            status: 500,
            body: { error: "Main function not defined" }
        };
    }

    try {
        return {
            status: 200,
            body: await main(input)
        }
    }
    catch (error) {
        console.error("Function error:", error.stack);
        return {
            status: 500,
            body: {
                error: "Processing failed",
                message: error.message,
                stackTrace: error.stack,
            }
        };
    }
};
